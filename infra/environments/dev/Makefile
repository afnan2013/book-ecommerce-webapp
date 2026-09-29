.PHONY: init plan apply destroy

init:
	terraform init \
	-backend-config="bucket=afnan-s3-connector" \
	-backend-config="key=book-ecommerce/dev/terraform.tfstate" \
	-backend-config="region=eu-west-1" \
	-backend-config="encrypt=true" \
	-backend-config="use_lockfile=true"

plan:
	terraform plan -var-file="env.tfvars" -out="terraform.tfplan"

apply:
	terraform apply "terraform.tfplan"

destroy:
	terraform plan -destroy -var-file="env.tfvars" -out="terraform.tfplan"