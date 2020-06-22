# Description: This script will create a user object in Active Directory for the Listener Service's Service Account
#              and also register the user in Safefuard. This is for the Active Roles/Safeguard Just-In-Time account enablement process.
#
# Pre-Requisites: The Active Roles Management Tools must be installed on the system where this script is executed
#                 The Safeguard PowerShell Module must be installed: Install-Module safeguard-ps
#                 This script must be executed by someone with administrative rights to Active Roles
#                 The credentials of a local Safeguard admin will need to be entered when prompted by the script
#                 The domain where the user object this script creates must be a Managed Domain in Active Roles as well as a registered AD domain within Safeguard
#
# Parameters: -Domain   : Specify the NetBIOS or FQDN of the Managed Domain where the user is to be created
#             -Username : Specify the samAccountName of the Service Account user to be created in the default Users container in AD
#             -Safeguard: Specify a DNS name or IP address of Safeguard appliance to register AD user created by this script
#
# Example: Create-ARSGJITUser -Domain [domain.fqdn|domainNetBIOS] -UserName UserSamAccountName -Safeguard [SPPDNSName|IPAddress]
#
##################

param ([Parameter(Mandatory=$true)]
       [ValidateNotNullOrEmpty()]
       [String]
       $Domain,

       [Parameter(Mandatory=$true)]
       [ValidateNotNullOrEmpty()]
       [String]
       $UserName,
       
       [Parameter(Mandatory=$true)]
       [ValidateNotNullOrEmpty()]
       [String]
       $Safeguard)

##########
# Define functions
##########
function ScriptCleanUp()
{
   # This function will reset variables and close connections
   Write-Host "Cleaning up..."
   Disconnect-QADService
   Disconnect-Safeguard
   $script:ARConnection = $null
   $script:DomainName = $null
   $script:DomainDN = $null
   $script:DomainDN = $null
   $script:objDomain = $null
   $script:DomainTypeEntered = $null
   $script:proceed = $null
   $script:objUser = $nul
   $script:objMD = $null
   $script:SGADAsset = $null
   $script:SGADAssetAcct = $null
   $script:newuser = $null
   $script:userproperties = $null
   $script:strPwd = $null
   $script:objADCred = $null
   $script:objSGCred = $null
}
#########

# Declare/Set variables
$ADUserExists = $false

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

# Check format of Domain name entered
if ($Domain.Contains("."))
{
   $DomainTypeEntered = "FQDN"
   # FQDN entered, check if a Managed Domain from connection
   $objMD = $ARConnection.ManagedDomains | select name,dnsname,DN | where {$_.dnsname -eq $Domain}
}
else
{
   $DomainTypeEntered = "NetBIOS"
   # NetBIOS entered, check if a Managed Domain from connection
   $objMD = $ARConnection.ManagedDomains | select name,dnsname,DN | where {$_.name -eq $Domain}
}

# Check if a matching Managed Domain object was located
if ($objMD)
{
   $DomainDN  = $objMD.DN
   $DomainDNS = $objMD.dnsname
   Write-Host "Managed Domain was located within Active Roles: $DomainDN"
}
else
{
   Write-Host "Managed Domain could not be located in Active Roles for $DomainTypeEntered domain name entered: $Domain, please try again, script terminating"
   ScriptCleanUp
   break
}

# Prompt user for Safeguard credentials
Write-Host "Please enter credentials to connect to Safegaurd appliance: $Safeguard"

try
{
   $objSGCred = Get-Credential -Message "Please enter Safeguard Local credentials"
   Connect-Safeguard -Appliance $Safeguard -Insecure -NoWindowTitle -IdentityProvider "local" -Credential $objSGCred
}
catch
{
   Write-Host "An error occurred trying to connect to Safeguard, script terminating"
   $_.Exception.Message
   $_.Exception.ItemName
   ScriptCleanUp
   break
}

# Check for registered AD domain Asset within Safeguard
try
{
   $SGADAsset = (Get-SafeguardDirectory -Fields ID, Name, Domains.DomainName) | where {$_.Domains.DomainName -eq $DomainDNS}
}
catch
{
   Write-Host "An error occurred retrieving AD asset information from Safeguard, script terminating"
   $_.Exception.Message
   $_.Exception.ItemName
   ScriptCleanUp
   break
}

# Check if domain is a registered Asset in Safeguard
If ($SGADAsset)
{
   Write-Host "Domain: $DomainDNS was located as a registered domain Asset within Safeguard"
}
else
{
   Write-Host "Domain: $DomainDNS was not located as a registered domain within Safeguard, script terminating"
   ScriptCleanUp
   break
}

Write-Host "Searching for user: $UserName within Active Roles Managed Domain: $DomainDN"

# Connect to Active Roles and check if the username already exists
$objUser = Get-QADUser -Connection $ARConnection -Identity $UserName -SearchRoot $DomainDN

if ($objUser)
{
   # User was found in AD, next check Safeguard 
   Write-Host "The UserName entered: $UserName already exists in domain $DomainDN"
   Write-Host "Searching Safeguard for regsitered Asset Account: $UserName"

   try
   {
      $SGADAssetAcct = Get-SafeguardDirectoryAccount -DirectoryToGet $SGADAsset.ID -AccountToGet $UserName -Fields Name
   }
   catch
   {
      Write-Host "The UserName specified: $UserName does not exist as an Asset Account within Safegaurd registered to domain Asset: $DomainDNS"
   }

   if ($SGADAssetAcct)
   {
      # User was found in Safeguard
      Write-Host "The UserName specified: $UserName exists in both Active Directory and Safeguard, there is nothing to do, script terminating"
      ScriptCleanUp
      break
   }
   $ADUserExists = $true
}
else
{
   Write-Host "The UserName entered: $UserName does not exist in domain: $DomainDN, it will be created"
}

# Prompt to proceed with Safeguard registration if AD user exists
if ($ADUserExists -eq $true)
{
   Write-Host "Prompting user to continue with Safeguard service account registration"
   # Setup user prompt
   $yes = New-Object System.Management.Automation.Host.ChoiceDescription "&Yes","Description."
   $no = New-Object System.Management.Automation.Host.ChoiceDescription "&No","Description."
   $cancel = New-Object System.Management.Automation.Host.ChoiceDescription "&Cancel","Description."
   $options = [System.Management.Automation.Host.ChoiceDescription[]]($yes, $no, $cancel)

   ## Use the following each time your want to prompt the use
   $title = "User already exists"
   $message = "The UserName entered: $UserName already exists in domain: $DomainDN. Would you like to continue with the registration of this user in Safeguard?"
   $result = $host.ui.PromptForChoice($title, $message, $options, 1)
   switch ($result)
   {
      0 {
         # Yes
         $proceed = $true
         break
      }
      1 {
         # No
         $proceed = $false
         break
      }
      2 {
         # Cancel
         $proceed = $false
         break
      }
   }
}

# Check user response
if ($proceed -eq $false)
{
   Write-Host "script terminating"
   ScriptCleanUp
   break
}

# Continue with AD user creation if user does not exist
if ($ADUserExists -eq $false)
{
   # Generate random complex initial password for user
   $strPwd = ([char[]]([char]33..[char]95) + ([char[]]([char]97..[char]126)) + 0..9 | sort {Get-Random})[0..31] -join ''

   # Construct the DN location where the user will be created
   $strUserContainerDN = "CN=Users," + $DomainDN

   # Populate attributes for new user object
   $UserProperties = New-Object PSObject -Property @{
      Description = "Auto-generated Service Account for Active Roles and Safeguard JIT Listener Service"
      givenName = "AR-SG Listener"
      sn = "Service Account"
      userPrincipalName = $UserName + "@" + $objMD.dnsname}

   # Attempt to create the user
   try
   {
      $newuser = New-QADUser -Connection $ARConnection -ParentContainer $strUserContainerDN -Name $UserName -SamAccountName $UserName -UserPassword $strPwd -ObjectAttributes $UserProperties
   }
   catch
   {
      Write-Host "An error occurred while trying to create user: $UserName, in Managed Domain: $DomainDN, please try again, script terminating"
      $_.Exception.Message
      $_.Exception.ItemName
      ScriptCleanUp
      break
   }
   Write-Host "User: $UserName successfully created in Active Directory"
}
else
{
   # AD user already existed in AD when running this script, prompt for current AD user's password to register in Safeguard
   # Construct service account username for PSCred dialog
   $ADCredUser = $objMD.Name + "\" + $UserName
   # Prompt user to enter the password for the existing service account
   $objADCred = Get-Credential -username $ADCredUser -Message "Please enter the AD password for the existing service account"

   # Retrieve the password from the credential object
   $strPwd = $objADCred.GetNetworkCredential().Password

   # Attempt a quick query of the domain to verify credentials
   $CurrentDomain = "LDAP://" + $objMD.DN
   $CredCheck = New-Object System.DirectoryServices.DirectoryEntry($CurrentDomain, $ADCredUser, $strPwd)

   if ($CredCheck.name -eq $null)
   {
      Write-Host "Authentication failed - prompting again to verify password"
      $objADCred = Get-Credential -username $ADCredUser -Message "Authentication failed - Please verify the password again"
      $strPwd = $objADCred.GetNetworkCredential().Password

      # Verify credentials one more time
      $CredCheck = New-Object System.DirectoryServices.DirectoryEntry($CurrentDomain, $ADCredUser, $strPwd)
      if ($CredCheck.name -eq $null)
      {
         Write-Host "Authentication failed again, please re-verify credentials and run this script again, script terminating"
         ScriptCleanUp
         break
      }
      else
      {
         Write-Host "Service account credentials successfully verified with domain: $DomainDNS"
      }
   }          
   else
   {
      Write-Host "Service account credentials successfully verified with domain: $DomainDNS"
   }

}

Write-Host "Registering user: $UserName as an Asset Account in Safeguard for domain Asset: $DomainDNS"

try
{
   $SGADAssetAcct = New-SafeguardAssetAccount -ParentAsset $SGADAsset.ID -NewAccountName $UserName -DomainName $DomainDNS
   # Set the password in Safeguard to be the same password as in AD
   Set-SafeguardAssetAccountPassword -AccountToSet $SGADAssetAcct.Id -NewPassword (ConvertTo-SecureString -String $strPwd -AsPlainText -Force)
}
catch
{
   Write-Host "An error occurred registering user: $UserName on Safeguard appliance: $Safeguard, script terminating"
   $_.Exception.Message
   $_.Exception.ItemName
   ScriptCleanUp
   break
}

Write-Host "User: $UserName successfully added as an Asset Account to domain Asset: $DomainDNS in Safeguard, script complete"

ScriptCleanUp
Write-Output "Done"