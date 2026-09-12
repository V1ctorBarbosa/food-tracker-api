terraform {
  required_version = ">= 1.7"

  cloud {
    organization = "food-track"

    workspaces {
      name = "food-track-api"
    }
  }

  required_providers {
    aws = {
      source = "hashicorp/aws"
      # ~> 5.0 (resolved to 5.100.0) rejected runtime = "dotnet10" -- its vendored AWS SDK
      # predates AWS adding that runtime in January 2026. Confirmed 6.64.0 vendors
      # aws-sdk-go-v2/service/lambda v1.107.0, which does have it.
      version = "~> 6.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}
