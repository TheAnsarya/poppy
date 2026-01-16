# 🌸 Publish Poppy Assembly Extension to VS Code Marketplace

## Prerequisites

1. **Azure DevOps Personal Access Token (PAT)**
   - Go to: https://dev.azure.com/TheAnsarya/_usersSettings/tokens
   - Or: https://marketplace.visualstudio.com/manage/publishers/TheAnsarya
   - Click "New Token"
   - Set **Organization** to "All accessible organizations"
   - Set **Scopes** to "Custom defined" and select:
     - **Marketplace** → **Manage** (full access)
   - Copy the generated token (you won't see it again!)

## Publishing Steps

Run these commands in the `vscode-extension` directory:

```powershell
cd c:\Users\me\source\repos\poppy\vscode-extension

# Step 1: Login with your PAT (paste token when prompted)
vsce login TheAnsarya

# Step 2: Package the extension (creates .vsix file)
vsce package

# Step 3: Publish to marketplace
vsce publish
```

## Alternative: Publish with Token in One Command

```powershell
# If you have the PAT ready, you can publish directly:
vsce publish -p <YOUR_PAT_TOKEN>
```

## Verify Publication

After publishing, verify at:
https://marketplace.visualstudio.com/items?itemName=TheAnsarya.poppy-assembly

## Troubleshooting

- **Timeout errors**: Try again, Azure DevOps can be slow
- **401 Unauthorized**: PAT may have expired or wrong scopes
- **Publisher not found**: Create publisher at https://marketplace.visualstudio.com/manage/publishers

## Current Extension Info

- **Name**: poppy-assembly
- **Version**: 1.0.0
- **Publisher**: TheAnsarya
- **Display Name**: Poppy Assembly

