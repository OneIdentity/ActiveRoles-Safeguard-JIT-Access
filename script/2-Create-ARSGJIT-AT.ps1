# Description: This script will create the Access Template container and and Access Temaplte required for the Active Roles/Safeguard Just-In-Time account enablement process
#
# Pre-Requisites: The Active Roles Management Tools must be installed on the system where this script is executed
#                 This script must be executed by someone with administrative rights to Active Roles
#                 The (default) name of the Virtual Attribute created by the Create-ARSGJIT-VA.ps1 script is hard coded in this script to be: edsva-SG_IsSafeguardRequestAvailable
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
   $script:ATExists = $null
   $script:strATRootContainerDN = $null
   $script:strATContainerName = $null
   $script:strATName = $null
   $script:strATContainerFullDN = $null
   $script:strATFullDN = $null
   $script:objAT = $null
   $script:NewATE = $null
   
}
#########

# The DN location where all Access Templates are created
#$root = [ADSI]"EDMS:"
$strATRootContainerDN = "CN=Access Templates,CN=Configuration"
# Name for the AT Container
$strATContainerName = "One Identity"
$strATName = "ActiveRoles-Safeguard-JIT"
$strVAName = "EDSVA-ARSGJitAccess"
# Combine vareibale to form complete VA DN
$strATContainerFullDN = "CN=" + $strATContainerName + "," + $strATRootContainerDN
$strATFullDN = "CN=" + $strATName + "," + $strATContainerFullDN

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

# Check if the Access Template Container needs to be created
# Display output to screen
Write-Output "Checking if the AR-SG JIT Access Template Container exists:"
$strATContainerFullDN

$ATContainerExists = $null
try
{
   $ATContainerExists = Get-QADObject -Connection $ARConnection -Identity $strATContainerFullDN
   if ($ATContainerExists -ne $null)
   {
      Write-Output "The AR-SG JIT Access Template Container already exists, checking for the Access Template:"
      $strATFullDN

      $ATExists = $null
      try
      {
         $ATExists = Get-QADObject -Connection $ARConnection -Identity $strATFullDN
         Write-Output "The AR-SG JIT Access Template already exists, exiting script"
      }
      catch
      {
         #$ErrorMessage = $_.Exception.Message
         #$FailedItem = $_.Exception.ItemName
         Write-Output "The AR-SG JIT Access Template does not appear to exist"
      }
   }
}
catch
{
   #$ErrorMessage = $_.Exception.Message
   #$FailedItem = $_.Exception.ItemName
   Write-Output "The AR-SG JIT Access Template Container does not appear to exist"
}

# Create new AT Container if it doesn't already exist
if ($ATContainerExists -eq $null)
{
   # Create new AT container
   Write-Output "Creating the Access Template Container: $strATContainerName"
   
   # Bind to the root AT container
   $objATRootContainer = [ADSI] "EDMS://$($strATRootContainerDN)"
   
   $newATContainer = $objATRootContainer.Create("edsAccessTemplatesContainer", "CN=" + $strATContainerName)

   # Commit the change to Active Roles to create the AT Container
   $newATContainer.SetInfo()
   Write-Output "AR-SG JIT Access Template Container information submitted to Active Roles"

   Write-Output "Checking if the AR-SG JIT Access Template Container was created"

   try
   {
      $ATContainerExists = Get-QADObject -Connection $ARConnection -Identity $strATContainerFullDN
      if ($ATContainerExists -ne $null){Write-Output "The AR-SG JIT Access Template Container has been successfully created"}
   }
   catch
   {
      #$exists = $null
      Write-Output "The AR-SG JIT Access Template Container was not created, an error occurred with this script, exiting script"
      ScriptCleanUp
      break
   }
}

# Create new AT if it doesn't already exist
if ($ATExists -eq $null)
{
   Write-Output "Creating the Access Template: $strATName"
   # Create AT in new AT container
   # Bind to the new AT container
   $objATContainer = [ADSI] "EDMS://$($strATContainerFullDN)"
   #$objATContainer = $root.OpenDSObject("EDMS://CN=One Identity,CN=Access Templates,CN=Configuration", $null, $null, 32768)

   $objNewAT = $objATContainer.Create("edsAccessTemplate", "CN=$strATName")
   $objNewAT.Put("Description", "Auto-generated Access Template for Active Roles and Safeguard JIT integration")
   # Commit the change to Active Roles to create the AT
   $objnewAT.SetInfo()

   Write-Output "AR-SG JIT Access Template information submitted to Active Roles"
   Write-Output "Checking if the AR-SG JIT Access Template was created"

   $ATExists = $null
   try
   {
      $ATExists = Get-QADObject -Connection $ARConnection -Identity $strATFullDN
      if ($ATExists -ne $null){Write-Output "The AR-SG JIT Access Template was created"}
   }
   catch
   {
      #$exists = $null
      Write-Output "The AR-SG JIT Access Template was not created, an error occurred with this script, exiting script"
      ScriptCleanUp
      break
   }

   Write-Output "Next, adding permissions entries to the Access Template"
   # Create AT permission entries
   # Constants
   $EDS_RIGHT_DS_READ_PROP = 16
   $EDS_RIGHT_DS_WRITE_PROP = 32
   $EDS_RIGHT_DS_LIST_OBJECT = 128

   # Allow access (as opposed to a Deny access)
   $ACCESS_ALLOWED_ACE_TYPE = 0

   # Bind to the Access Template
   $objAT = [ADSI] "EDMS://$($strATFullDN)"
   #$objAT = $root.OpenDSObject("EDMS://$strATFullDN", $null, $null, 32768)

   # Create a new Permission Entry, a new permission entry is needed for each entry in the AT
   $NewATE = $objAT.CreatePermissionEntry()

   # Set properties of the newly created Permission Entry: Allow - Read all properties - User objects
   $NewATE.AteType = $ACCESS_ALLOWED_ACE_TYPE
   $NewATE.AccessMask = $EDS_RIGHT_DS_READ_PROP
   $NewATE.InheritedObjectTypeADsPath = "EDMS://user,Schema"

   # Add Permission Entry to Access Template, reset NewATE variable
   $objAT.AddPermissionEntry($NewATE)
   $NewATE = $null

   # Create a new Permission Entry
   $NewATE = $objAT.CreatePermissionEntry()

   # Set properties of the newly created Permission Entry: Allow - Write edsva-SG_IsSafeguardRequestAvailable - User objects
   # ObjectTypeADsPath is the specific attribute to delegate permissions to
   # If not specified, then the permission entry applies to all properties of the object type indicated by the InheritedObjectTypeADsPath object
   $NewATE.AteType = $ACCESS_ALLOWED_ACE_TYPE
   $NewATE.AccessMask = $EDS_RIGHT_DS_WRITE_PROP
   $NewATE.InheritedObjectTypeADsPath = "EDMS://user,Schema"
   $NewATE.ObjectTypeADsPath = "EDMS://$strVAName,Schema"

   # Add Permission Entry to Access Template
   $objAT.AddPermissionEntry($NewATE)
   $NewATE = $null

   # Create a new Permission Entry, a new entry is needed for each entry in the AT
   $NewATE = $objAT.CreatePermissionEntry()

   # Set properties of the newly created Permission Entry: Allow - List Object - Domain objects
   $NewATE.AteType = $ACCESS_ALLOWED_ACE_TYPE
   $NewATE.AccessMask = $EDS_RIGHT_DS_LIST_OBJECT
   $NewATE.InheritedObjectTypeADsPath = "EDMS://domain,Schema"

   # Add Permission Entry to Access Template
   $objAT.AddPermissionEntry($NewATE)
   $NewATE = $null

   # Create a new Permission Entry, a new entry is needed for each entry in the AT
   $NewATE = $objAT.CreatePermissionEntry()

   # Set properties of the newly created Permission Entry: Allow - List Object - User objects
   $NewATE.AteType = $ACCESS_ALLOWED_ACE_TYPE
   $NewATE.AccessMask = $EDS_RIGHT_DS_LIST_OBJECT
   $NewATE.InheritedObjectTypeADsPath = "EDMS://user,Schema"

   # Add Permission Entry to Access Template
   $objAT.AddPermissionEntry($NewATE)
   $NewATE = $null

   # Commit changes to add permission entries to AT
   $objAT.SetInfo()

   Write-Output "All Access Template permission entries were successfully created, script complete"
}

ScriptCleanUp
Write-Output "Done"