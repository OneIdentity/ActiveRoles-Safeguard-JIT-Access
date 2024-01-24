# ActiveRoles and Safeguard for Just-in-time Access
Active Roles [Just-in-Time Provisioning](https://www.oneidentity.com/what-is-just-in-time-provisioning/) for Safeguard allows privileges to be assigned at the time of a credential check-out.  Accounts in AD that will require privileges to perform a function (i.e. Domain Admin) will be added to the appropriate group(s) when the the account is approved for check out in Safeguard.  This applies to both password requests and session requests. 

This is accomplished by adding the "to be privileged" user account to the appropriate group as required, then removed when no longer required.  This further secures the AD environment from privilege escalation attacks.

This project provides a simple listener service, `ARSGJitAccess`, which subscribes to Safeguard password request events. When a PasswordRequest is approved by Safeguard, the `ARSGJitAccess` calls Active Roles to set a (configurable) `ARSGJITAccessAttribute` on the privileged account. Active Roles is configured to make dynamic group membership changes based on the value of the `ARSGJITAccessAttribute` 

## Watch the demo
Please watch the following video to see a demonstration of the just-in-time access solution:
* [Demo Video](https://oneidentity.github.io/ActiveRoles-Safeguard-JIT-Access/demo.html "Demo Video")

## Install and Config
* [Full Install and Config Video](https://oneidentity.github.io/ActiveRoles-Safeguard-JIT-Access/install.html "Install Video")
* [Download the installer and scripts](https://github.com/OneIdentity/ActiveRoles-Safeguard-JIT-Access/releases "Release")

## Installing multiple instances
Some users may desire to install multiple instances of Active Roles Just-in-Time Provisioning for Safeguard services for situations where different configurations are needed:
* Having multiple services to toggle different ARS Attributes
* Connecting to different Safeguard for Privileged Password appliances
* Using different ARS or Safeguard service accounts. 
````
# Create config file from configuration workflow
ARSGJITAccess.exe -config <file path>

# Manually Start Service with Custom Config File
ARSGJITAccess.exe -ConfigFile <path_to_config_file>

# Install Multiple Instances
ARSGJitAccess.exe -installAndConfigureInstance "<instance_name>"

# (Uninstall an instance use)
ARSGJITAccess.exe -uninstallService "<instance_name>"
````

## Versions
You should use the [release](https://github.com/OneIdentity/ActiveRoles-Safeguard-JIT-Access/releases) of Safeguard JIT Access that matches the major.minor version of Safeguard SPP that you are using. New features and bug fixes will be made only for the most current version of Safeguard.  

## Support
One Identity open source projects are supported through One Identity GitHub issues and the One Identity Community. This includes all scripts, plugins, SDKs, modules, code snippets or other solutions. For assistance with any One Identity GitHub project, please raise a new Issue on the One Identity GitHub project page. You may also visit the One Identity Community to ask questions. Requests for assistance made through official One Identity Support will be referred back to GitHub and the One Identity Community forums where those requests can benefit all users.
