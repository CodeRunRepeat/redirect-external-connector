az login --allow-no-subscriptions

tenantId=$(az account show --query tenantId -o tsv)
echo "tenantId: $tenantId"

appName="sample-graph-connector"
appId=$(az ad app create --display-name $appName --query appId -o tsv)
echo "appId: $appId"

# Remove the default User.Read permission
echo "Removing User.Read permission..."
userReadPermissionId=$(az ad app permission list --id $appId --query "[?resourceDisplayName=='Microsoft Graph' && permissionName=='User.Read'].id" -o tsv)
if [ -n "$userReadPermissionId" ]; then
    az ad app permission delete --id $appId --api 00000003-0000-0000-c000-000000000000 --api-permissions $userReadPermissionId
fi

# --------------- Permissions do not work currently ---------------
# --------------- Add them manually in the Azure Portal -----------
# Add new permissions
echo "Adding ExternalConnection.ReadWrite.OwnedBy and ExternalItem.ReadWrite.OwnedBy permissions..."
az ad app permission add --id $appId --api 00000003-0000-0000-c000-000000000000 --api-permissions f431331c-49a6-499f-be1c-62af19c34a9d=Scope 8116ae0f-55c2-452d-9944-d18420f5b2c8=Scope

# Grant admin consent for the added permissions
echo "Granting admin consent for the added permissions..."
az ad app permission grant --id $appId --api 00000003-0000-0000-c000-000000000000 --scope ExternalConnection.ReadWrite.OwnedBy ExternalItem.ReadWrite.OwnedBy

# Confirm the permissions have been added and granted
echo "Updated API permissions:"
az ad app permission list --id $appId --output table
# -----------------------------------------------------------------------

echo "Adding a client secret..."
clientSecret=$(az ad app credential reset --id $appId --append --query password -o tsv)
echo "export CLIENT_SECRET=$clientSecret" > ./.env

cd ../test-external-connector
dotnet user-secrets init
dotnet user-secrets set settings:clientId $appId
dotnet user-secrets set settings:tenantId $tenantId
dotnet user-secrets set settings:clientSecret $clientSecret