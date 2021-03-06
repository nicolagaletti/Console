<#
    .SYNOPSIS
        Allows to find items using the Sitecore Content Search API.

    .DESCRIPTION
        Allows to find items using the Sitecore Content Search API.


    .PARAMETER Criteria
        simple search criteria in the following example form:
        
        @{
            Filter = "Equals"
            Field = "_templatename"
            Value = "PowerShell Script"
        }, 
        @{
            Filter = "StartsWith"
            Field = "_fullpath"
            Value = "/sitecore/system/Modules/PowerShell/Script Library/System Maintenance"
        }

        Where "Filter" is one of the following values:
        - Equals
        - StartsWith,
        - Contains,
        - EndsWith
        
        Fields by which you can filter can be discovered using the following script:

        Find-Item -Index sitecore_master_index `
                  -Criteria @{Filter = "StartsWith"; Field = "_fullpath"; Value = "/sitecore/content/" } `
                  -First 1 | 
            select -expand "Fields"
        
    .PARAMETER Where
        Filtering Criteria using Dynamic Linq syntax: http://weblogs.asp.net/scottgu/dynamic-linq-part-1-using-the-linq-dynamic-query-library

    .PARAMETER WhereValues
        An Array of objects for Dynamic Linq "-Where" parameter as explained in: http://weblogs.asp.net/scottgu/dynamic-linq-part-1-using-the-linq-dynamic-query-library

    .PARAMETER OrderBy
        Field by which the search results sorting should be performed. 
        Dynamic Linq ordering syntax used.
        http://weblogs.asp.net/scottgu/dynamic-linq-part-1-using-the-linq-dynamic-query-library

    .PARAMETER First
        Number of returned search results.

    .PARAMETER Skip
        Number of search results to be skipped skip before returning the results commences.

    .PARAMETER Index
        Name of the Index to be used. Within the ISE index name autocompletion is provided. Press Ctrl+Space to be offered list of available indexes after typing "-Index".
    
    .INPUTS
    
    .OUTPUTS
        Sitecore.ContentSearch.SearchTypes.SearchResultItem

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Initialize-Item

    .LINK
        Get-Item

    .LINK
        Get-ChildItem

    .LINK
        https://gist.github.com/AdamNaj/273458beb3f2b179a0b6

    .LINK
        http://weblogs.asp.net/scottgu/dynamic-linq-part-1-using-the-linq-dynamic-query-library

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        # Fields by which filtering can be performed using the -Criteria parameter
        Find-Item -Index sitecore_master_index `
                  -Criteria @{Filter = "StartsWith"; Field = "_fullpath"; Value = "/sitecore/content/" } `
                  -First 1 | 
            select -expand "Fields"

    .EXAMPLE
        # Find all Template Fields using Dynamic LINQ syntax 
        Find-Item `
            -Index sitecore_master_index `
            -Where 'TemplateName = @0 And Language=@1' `
            -WhereValues "Template Field", "en"

    .EXAMPLE
        # Find all Template Fields using the -Criteria parameter syntax 
        Find-Item `
                -Index sitecore_master_index `
                -Criteria @{Filter = "Equals"; Field = "_templatename"; Value = "Template Field"},
                          @{Filter = "Equals"; Field = "_language"; Value = "en"}
#>
