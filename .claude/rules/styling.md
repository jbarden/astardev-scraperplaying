# Styling

## Expression-bodies

Use expression-bodies whenever possible:

- methods with a single instruction

### Single line methods / statements example

```csharp
if (!isRootDirectoryAvailable)
{
    AppendStatusMessage("Root directory could not be found.");
}
```

instead:

```csharp
if (!isRootDirectoryAvailable) AppendStatusMessage("Root directory could not be found.");
```
