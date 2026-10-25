# ADR 0001: Preserve the existing .NET desktop stack

Status: existing implementation preserved for upload preparation.

The application is C#/.NET 10/Avalonia. The owner's architecture template described Python tools, but also required preserving working behavior. Keep the existing stack and express future core/platform seams as C# interfaces. The upload bundle retains repository-relative paths so existing harnesses remain runnable. A larger source-layout migration remains a separate reviewed change.
