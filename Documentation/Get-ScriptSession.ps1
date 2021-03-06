<#
    .SYNOPSIS
        Returns the list of PowerShell Extension Sessions running in background.

    .DESCRIPTION
        Returns the list of PowerShell Extension Sessions running in background.


    .PARAMETER Id
        Id of the session.

    .PARAMETER Current
        Returns current script session if the session is run in a background job.
    
    .INPUTS
    
    .OUTPUTS
        Cognifide.PowerShell.PowerShellIntegrations.Host.ScriptSession        

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        PS master:\>Get-ScriptSession
         
        Type         Key                                                                              Location                                 Auto Disposed
        ----         ---                                                                              --------                                 -------------
        Console      $scriptSession$|zwlyrcmmzwisv22djsv0ej2a|8d5c3e63-3fed-0532-e7c5-761760567b83                                             False
        Context      $scriptSession$|zwlyrcmmzwisv22djsv0ej2a|renderingCopySession                    master:\content\Home                     False
        Context      $scriptSession$|zwlyrcmmzwisv22djsv0ej2a|packageBuilder                          master:\content\Home                     False

    .EXAMPLE
        PS master:\>Get-ScriptSession -Current
         
        Type         Key                                                                              Location                                 Auto Disposed
        ----         ---                                                                              --------                                 -------------
        Console      $scriptSession$|zwlyrcmmzwisv22djsv0ej2a|8d5c3e63-3fed-0532-e7c5-761760567b83                                             False
#>
