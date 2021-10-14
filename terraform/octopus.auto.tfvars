environment = "#{Environment}"

# Azure Service Principal
tenant_id        = "#{AzureAccount.TenantId}"
subscription_id  = "#{AzureAccount.SubscriptionNumber}"
client_id        = "#{AzureAccount.Client}"
client_secret    = "#{AzureAccount.Password}"
pfx_password     = "#{Certificate.Password}"
pfx_certificate  = "#{Certificate.Pfx}"