$resource_group_name = 'image-api-grp'
$appserviceplan_name = 'fasp'
$azure_function_name = 'image-api'
$storageaccount_name = 'tgimageapistorage'

Write-Host "Deploying resources in $resource_group_name"

# Create a new resource-group
az group create -l westeurope -n $resource_group_name

# Deploy resources inside resource-group
az deployment group create --mode Incremental --resource-group $resource_group_name --template-file template-azure-function.json --parameters appService_name=$azure_function_name appServicePlan_name=$appserviceplan_name resourceGroup=$resource_group_name storageaccount_name=$storageaccount_name

#az deployment group create --resource-group tri-inholland --template-file azure-function.json --parameters @azure-function.env.parameters.json

