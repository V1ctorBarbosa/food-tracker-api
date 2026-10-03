terraform {
  required_version = ">= 1.7"

  cloud {
    organization = "v1ctorbarbosa"

    workspaces {
      name = "food-track-api-bootstrap"
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
