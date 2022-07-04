terraform {
  backend "remote" {
    organization = "octopus-deploy"
    token = "#{TerraformCloudApiToken}"

    workspaces {
      name = "#{TerraformWorkspace}"
    }
  }

  required_providers {
    dnsimple = {
      source = "dnsimple/dnsimple"
    }
  }
}

variable "environment" {
  default = "Development"
}
variable "app_service_plan" {
    default = "octopus-coreplatformapps-development"
}
variable "tenant_id" {}
variable "subscription_id" {}
variable "client_id" {}
variable "client_secret" {}
variable "pfx_certificate" {
  description = "the pfx file for the SSL Certificate"
}
variable "pfx_password" {
  description = "the pfx password used to access the public SSL certificate"
}
variable "resource_group_name" {}
variable "dnsimple_token" {}
variable "dnsimple_account" {}

provider "azurerm" {
  features {}

  tenant_id       = var.tenant_id
  subscription_id = var.subscription_id
  client_id       = var.client_id
  client_secret   = var.client_secret
}

provider "dnsimple" {
  token   = var.dnsimple_token
  account = var.dnsimple_account
}

resource "azurerm_resource_group" "group" {
  name = var.resource_group_name
  location = "Australia East"
  tags = {
      "WorkloadName" = "TeamcityToGithub",
      "ApplicationName" = "GithubStatusChecks",
      "BusinessUnit" = "Core Platform",
      "Team" = "#team-core-platform",
      "Criticality" = "mission-critical"
  }
}

resource "azurerm_service_plan" "plan" {
  name                = var.app_service_plan
  location            = azurerm_resource_group.group.location
  resource_group_name = azurerm_resource_group.group.name
  os_type             = "Windows"
  sku_name            = "B1"
}

resource "azurerm_windows_web_app" "web" {
  name                = "github-status-checks-${lower(var.environment)}-core-platform"
  location            = azurerm_service_plan.plan.location
  resource_group_name = azurerm_resource_group.group.name
  service_plan_id     = azurerm_service_plan.plan.id
  https_only          = true

  site_config {
    application_stack {
      current_stack = "dotnet"
      dotnet_version = "v6.0"
    }
  }
}

resource "azurerm_app_service_certificate" "ssl" {
  name                = "GithubStatusChecks-ssl-${var.environment}"
  location            = azurerm_resource_group.group.location
  resource_group_name = azurerm_resource_group.group.name
  pfx_blob            = var.pfx_certificate
  password            = var.pfx_password
}

resource "dnsimple_zone_record" "verify_entry" {
  zone_name = "octopushq.com"
  name      = "asuid.githubstatuschecks-coreplatform"
  type      = "TXT"
  value     = azurerm_windows_web_app.web.custom_domain_verification_id
}

resource "dnsimple_zone_record" "domain_entry" {
  zone_name = "octopushq.com"
  name      = "githubstatuschecks-coreplatform"
  type      = "CNAME"
  value     = azurerm_windows_web_app.web.default_hostname
}

resource "azurerm_app_service_custom_hostname_binding" "web_app_binding" {
  hostname            = "githubstatuschecks-coreplatform.octopushq.com"
  app_service_name    = azurerm_windows_web_app.web.name
  resource_group_name = azurerm_resource_group.group.name

  depends_on = [
    dnsimple_zone_record.verify_entry,
    dnsimple_zone_record.domain_entry
  ]

  lifecycle {
    ignore_changes = [ssl_state, thumbprint]
  }
}

resource "azurerm_app_service_certificate_binding" "cert_binding" {
  hostname_binding_id = azurerm_app_service_custom_hostname_binding.web_app_binding.id
  certificate_id      = azurerm_app_service_certificate.ssl.id
  ssl_state           = "SniEnabled"
}
