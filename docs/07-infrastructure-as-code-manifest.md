# Infrastructure-as-Code (IaC) Specifications

## 1. Terraform Regional Constraints
To ensure **FINMA Data Residency** compliance, the Terraform provider is locked to Swiss regions.

```hcl
# Example Constraint in main.tf
resource "azurerm_resource_group" "nexus_rg" {
  name     = "rg-ares-nexus-prod"
  location = "switzerlandnorth" # Hard constraint for Swiss Data Sovereignty
  tags = {
    Environment = "Production"
    Compliance  = "ISO27001"
    DataClass   = "Highly-Confidential"
  }
}
