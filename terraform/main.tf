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
    default = "octopus-engprodapps-development"
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
  name = "GithubStatusChecks-${var.environment}"
  location = "Australia East"
  tags = {
      "WorkloadName" = "TeamcityToGithub",
      "ApplicationName" = "GithubStatusChecks",
      "BusinessUnit" = "Engineering Productivity",
      "Team" = "#team-engineering-productivity",
      "Criticality" = "mission-critical"
  }
}

resource "azurerm_app_service_plan" "plan" {
  name                = var.app_service_plan
  location            = azurerm_resource_group.group.location
  resource_group_name = azurerm_resource_group.group.name

  sku {
    tier = "Basic"
    size = "B1"
  }
}

resource "azurerm_app_service" "web" {
  name                = "github-status-checks-${lower(var.environment)}"
  location            = azurerm_resource_group.group.location
  resource_group_name = azurerm_resource_group.group.name
  app_service_plan_id = azurerm_app_service_plan.plan.id
  https_only          = true

  site_config {
    dotnet_framework_version = "v5.0"
  }
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
  app_service_name    = azurerm_app_service.web.name
  resource_group_name = azurerm_app_service.web.resource_group_name
}