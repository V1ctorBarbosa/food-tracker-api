terraform {
  required_version = ">= 1.7"

  cloud {
    # TODO: replace with your Terraform Cloud organization name (same one used in bootstrap).
    organization = "REPLACE_WITH_TFC_ORG"

    workspaces {
      name = "food-track-api"
    }
  }

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}
