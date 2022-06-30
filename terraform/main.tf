terraform {
  backend "remote" {
    organization = "octopus-deploy"
    token = "#{TerraformCloudApiToken}"

    workspaces {
      name = "#{TerraformWorkspace}"
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

provider "azurerm" {
  features {}

  tenant_id       = var.tenant_id
  subscription_id = var.subscription_id
  client_id       = var.client_id
  client_secret   = var.client_secret
}

resource "azurerm_resource_group" "group" {
  name = "#{ResourceGroupName}"
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

  application_stack {
    current_stack = "dotnet"
    dotnet_version = "v6.0"
  }

  site_config {}
}

resource "azurerm_app_service_certificate" "ssl" {
  name                = "GithubStatusChecks-ssl-${var.environment}"
  location            = azurerm_resource_group.group.location
  resource_group_name = azurerm_resource_group.group.name
  pfx_blob            = var.pfx_certificate
  password            = var.pfx_password
}

resource "azurerm_app_service_custom_hostname_binding" "web" {
  count               = var.environment == "Production" ? 1 : 0
  hostname            = "githubstatuschecks.octopushq.com"
  app_service_name    = azurerm_windows_web_app.web.name
  resource_group_name = azurerm_windows_web_app.web.resource_group_name
  ssl_state           = "SniEnabled"
  thumbprint          = azurerm_app_service_certificate.ssl.thumbprint
}
