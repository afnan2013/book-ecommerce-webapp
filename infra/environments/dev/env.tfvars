name_prefix = "be-dev-1"
env         = "dev"
region      = "ap-south-1"

vpc_cidr = "10.0.0.0/16"

public_subnets = {
  a = { cidr = "10.0.1.0/24", az = "ap-south-1a" }
  b = { cidr = "10.0.2.0/24", az = "ap-south-1b" }
}

private_subnets = {
  a = { cidr = "10.0.11.0/24", az = "ap-south-1a" }
  b = { cidr = "10.0.12.0/24", az = "ap-south-1b" }
}