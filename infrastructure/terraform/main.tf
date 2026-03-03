resource "azurerm_resource_group" "aresnexus" {
  name     = "rg-aresnexus-prod"
  location = "Switzerland North"
}

resource "azurerm_kubernetes_cluster" "aks" {
  name                = "aks-aresnexus"
  location            = azurerm_resource_group.aresnexus.location
  resource_group_name = azurerm_resource_group.aresnexus.name
  dns_prefix          = "aresnexus"

  default_node_pool {
    name       = "default"
    node_count = 3
    vm_size    = "Standard_DS2_v2"
  }

  identity {
    type = "SystemAssigned"
  }

  tags = {
    Environment = "Production"
    Project     = "Ares-Nexus"
    Compliance  = "FINMA-DORA"
  }
}
