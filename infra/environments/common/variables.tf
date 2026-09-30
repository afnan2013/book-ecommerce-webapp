variable "project" {
  type    = string
  default = "book-ecommerce"
}

variable "name_prefix" {
  type    = string
  default = "be"
}

variable "env" {
  type = string
}

variable "region" {
  type = string
}

variable "vpc_cidr" {
  type = string
}

variable "public_subnets" {
  type = map(object({
    cidr = string
    az   = string
  }))
}

variable "private_subnets" {
  type = map(object({
    cidr = string
    az   = string
  }))
}

variable "db_allocated_storage" {
  type = number
}

variable "db_instance_class" {
  type = string
}

variable "db_deletion_protection" {
  type = bool
}

variable "db_backup_retention_days" {
  type = number
}

variable "db_skip_final_snapshot" {
  type = bool
}

variable "dns_zone_name" {
  type = string
}

variable "api_domain" {
  type = string

}