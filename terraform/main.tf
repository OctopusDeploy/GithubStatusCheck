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

provider "azurerm" {
  features {}

  tenant_id       = var.tenant_id
  subscription_id = var.subscription_id
  client_id       = var.client_id
  client_secret   = var.client_secret
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

resource "azurerm_resource_group" "github-status-checks" {
  name = "GithubStatusChecks-${var.environment}"
  location = azurerm_app_service_plan.plan.location
  tags = {
      "WorkloadName" = "TeamcityToGithub",
      "ApplicationName" = "GithubStatusChecks",
      "BusinessUnit" = "Engineering Productivity",
      "Team" = "#team-engineering-productivity",
      "Criticality" = "mission-critical"
  }
}

resource "azurerm_app_service" "web" {
  name                = "github-status-checks-${lower(var.environment)}"
  location            = azurerm_resource_group.github-status-checks.location
  resource_group_name = azurerm_resource_group.github-status-checks.name
  app_service_plan_id = azurerm_app_service_plan.plan.id
  https_only          = true

  site_config {
    dotnet_framework_version = "v5.0"
  }
}

resource "azurerm_app_service_custom_hostname_binding" "web" {
  count               = var.environment == "Production" ? 1 : 0
  hostname            = "githubstatuschecks.octopushq.com" 
  app_service_name    = azurerm_app_service.web.name
  resource_group_name = azurerm_app_service.web.resource_group_name
}