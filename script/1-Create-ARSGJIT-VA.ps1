# Description: This script will create the Virtual Attribute required for the Active Roles/Safeguard Just-In-Time account enablement process
#
# Pre-Requisites: The Active Roles Management Tools must be installed on the system where this script is executed
#                 This script must be executed by someone with administrative rights to Active Roles
#
# Parameters: None
#
##################

##########
# Define functions
##########
function ScriptCleanUp()
{
   # This function will reset variables and close connections
   Write-Host "Cleaning up..."
   Disconnect-QADService
   $script:ARConnection = $null
   $script:strVAContainerDN = $null
   $script:strVAName = $null
   $script:strVAFullDN = $null
   $script:strAttributeSyntax = $null
   $script:iOMSyntax = $null
   $script:strClassSchemas = $null
   $script:bisStored = $null
   $script:bisSingleValued = $null
   $script:objVaContainer = $null
   $script:objOctetString = $null
   $script:objNewVa = $null
   $script:objPolicyInfoList = $null
}
#########

# The DN location where all Virtual Attrbiutes are created
$strVAContainerDN = "CN=Virtual Attributes,CN=Server Configuration,CN=Configuration"
# lDAPDisplayName for the VA
$strVAName = "EDSVA-ARSGJitAccess"
# Combine vareibale to form complete VA DN
$strVAFullDN = "CN=" + $strVAName + "," + $strVAContainerDN

# Establish connection to Active Roles instance
Write-Host "Establishing connection to Active Roles"
try
{
   $AR = [ADSI]"EDMS://rootDSE"
   $ARService = $AR.psbase.InvokeGet("edsvaServiceFullDNS")
   $ARConnection = Connect-QADService -Proxy -Service $ARService  
}
catch
{
   Write-Host "An error occurred trying to connect to Active Roles, script terminating"
   ScriptCleanUp
   break
}

# Check if connection established
if ($ARConnection)
{
   Write-Host "Connection established to server: $ARService"
}
else
{
   Write-Host "An error occurred trying to connect to Active Roles, script terminating"
   ScriptCleanUp
   break
}

# Check if the Virtual Attribute needs to be created
# Display output to screen
Write-Output "Checking if the AR-SG JIT Virtual Attribute exists:"
$strVAFullDN

$exists = $null
try
{
   $exists = Get-QADObject -Connection $ARConnection -Identity $strVAFullDN
}
catch
{
   #$ErrorMessage = $_.Exception.Message
   #$FailedItem = $_.Exception.ItemName
   Write-Output "The AR-SG JIT Virtual Attribute does not appear to exist"
}

if ($exists -eq $null)
{
   Write-Output "Creating Virtual Attribute: $strVAName"
   # Virtual Attribute does not exist
   # Set the property attributeSyntax for the VA
   # 2.5.5.8 = Boolean; 2.5.5.1 = DN; 2.5.5.12 = DirectoryString
   $strAttributeSyntax = "2.5.5.8"

   # Set the property oMSyntax for the VA
   # 1 = Boolean; 127 = DN; 64 = DirectoryString
   $iOMSyntax = "1"

   # Set the object class to which the VA will apply
   $strClassSchemas = "user"

   # Specify whether to store the VA in the Active Roles database
   $bisStored = $true

   # Specify whether the VA is single-valued
   $bisSingleValued = $true

   # Bind to the VA container
   $objVaContainer = [ADSI] "EDMS://$($strVAContainerDN)"

   # Create a new Octet string object
   $objOctetString = New-Object -ComObject "AelitaEDM.EDMOctetString"

   # Create a new object of type edaVirtualAttribute
   $objNewVa = $objVaContainer.Create("edsVirtualAttribute", "CN=" + $strVAName)

   # Get the policy info list
   $objPolicyInfoList = $objNewVa.GetPolicyInfoList()

   # Retrieve the GUID value for schemaIDGUID from the policy info list information and set it on the Octet string
   $objOctetString.SetGuidString($objPolicyInfoList.Item("schemaIDGUID").GeneratedValue)

   # Set other attrbiutes relavent for the creation of the new VA
   $objNewVa.Put("edsaAttributeIsStored", $bisStored)
   $objNewVa.Put("isSingleValued", $bisSingleValued)
   $objNewVa.Put("lDAPDisplayName", $strVAName)
   $objNewVa.Put("edsaClassSchemas", $strClassSchemas)
   $objNewVa.Put("attributeSyntax", $strAttributeSyntax)
   $objNewVa.Put("oMSyntax", $iOMSyntax)
   $objNewVa.Put("schemaIDGUID", $objOctetString.GetOctetString())
   $objNewVa.Put("attributeID",$objPolicyInfoList.Item("attributeID").GeneratedValue)
   $objNewVa.Put("Description", "Auto-generated Virtual Attribute for Active Roles and Safeguard JIT integration")

   # Commit the change to Active Roles to create the VA
   $objNewVa.SetInfo()
   Write-Output "AR-SG JIT Virtual Attribute information submitted to Active Roles"

   # Clear variable
   $exists = $null

   Write-Output "Checking if the AR-SG JIT Virtual Attribute was created"

   try
   {
      $exists = Get-QADObject -Connection $ARConnection -Identity $strVAFullDN
      if ($exists -ne $null){Write-Output "The AR-SG JIT Virtual Attribute has been successfully created, script complete"}
   }
   catch
   {
      #$exists = $null
      Write-Output "The AR-SG JIT Virtual Attribute was not created, an error occurred with this script, exiting script"
   }
}
else
{
   Write-Output "The AR-SG JIT Virtual Attribute already exists, exiting script"
}

ScriptCleanUp
Write-Output "Done"
Write-Output "***NOTE: Please close this scripting environment and open a new session before attempting to run the script that creates the associated Access Template.***"