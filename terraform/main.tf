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

provider "azurerm" {
  features {}
}

// Use the shared app service plan
data "azurerm_app_service_plan" "microsites" {
  name                = "octopus-microsites-${lower(var.environment)}"
  resource_group_name = "Microsites-${var.environment}"
}

resource "azurerm_resource_group" "github-status-checks" {
  name = "GithubStatusChecks-${var.environment}"
  location = data.azurerm_app_service_plan.microsites.location
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
  app_service_plan_id = data.azurerm_app_service_plan.microsites.id
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