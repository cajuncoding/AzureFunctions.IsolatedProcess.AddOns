; Unshipped analyzer release
; https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
MAR001 | MiniApiRouting | Error | RouteHandler method must be declared in a MiniApi class.
MAR002 | MiniApiRouting | Error | Duplicate or ambiguous Mini API route.
MAR003 | MiniApiRouting | Error | Invalid RouteHandler method declaration.
MAR004 | MiniApiRouting | Error | Multiple request-body parameters.
MAR005 | MiniApiRouting | Error | Invalid optional route-parameter ordering.
MAR006 | MiniApiRouting | Error | MiniApiFunction references an unknown Mini API group.
MAR007 | MiniApiRouting | Error | RouteHandler parameter has no supported binding source.
MAR008 | MiniApiRouting | Error | Unsupported route constraint.
MAR009 | MiniApiRouting | Error | Catch-all route parameter is not terminal.
MAR010 | MiniApiRouting | Error | Parameter declares conflicting binding attributes.
MAR011 | MiniApiRouting | Error | Route token has no compatible binding destination.
MAR012 | MiniApiRouting | Error | Framework parameter declares body binding.
MAR013 | MiniApiRouting | Error | Query/header collection element type is unsupported.
MAR014 | MiniApiRouting | Error | Function method declares duplicate MiniApiFunction associations.
MAR015 | MiniApiRouting | Error | RouteHandler declares an invalid HTTP verb.
MAR016 | MiniApiRouting | Error | Route template syntax is invalid.