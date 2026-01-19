# 📦 VS Code Extension Publishing Guide

This guide covers packaging and publishing the Poppy Assembly VS Code extension.

---

## 🔧 Prerequisites

### Required Tools
```bash
# Install vsce (Visual Studio Code Extension manager)
yarn global add @vscode/vsce

# Verify installation
vsce --version
```

### Publisher Account

1. Create Microsoft account (if needed): <https://account.microsoft.com>
2. Create Azure DevOps organization: <https://dev.azure.com>
3. Create Personal Access Token (PAT):
   - Go to <https://dev.azure.com>
   - User Settings → Personal Access Tokens → New Token
   - Organization: **All accessible organizations**
   - Scopes: **Marketplace → Manage**
   - Copy token securely (shown only once!)

---

## 📦 Packaging

### 1. Pre-Package Checklist

Ensure all requirements are met:

- [ ] `package.json` version number updated
- [ ] `CHANGELOG.md` updated with new features
- [ ] All TypeScript code compiles without errors
- [ ] All tests pass (`yarn test`)
- [ ] README.md is complete and accurate
- [ ] Icon file exists (if specified in package.json)
- [ ] License file exists

### 2. Build Extension

```bash
cd vscode-extension

# Clean previous builds
rm -rf out/
rm -rf *.vsix

# Install dependencies
yarn install

# Compile TypeScript
yarn compile

# Verify compilation
ls out/
```

### 3. Create VSIX Package

```bash
# Package extension (creates .vsix file)
vsce package

# Output: poppy-assembly-0.1.0.vsix
```

**Common Issues:**

- **Missing README**: Ensure README.md exists and has content
- **Missing LICENSE**: Add LICENSE or MIT file
- **Missing icon**: Either add icon or remove from package.json
- **devDependencies in package**: Review .vscodeignore file

### 4. Test VSIX Locally

```bash
# Install packaged extension
code --install-extension poppy-assembly-0.1.0.vsix

# Test all features:
# 1. Open a .pasm file
# 2. Verify syntax highlighting works
# 3. Test IntelliSense (Ctrl+Space)
# 4. Test formatting (Shift+Alt+F)
# 5. Test snippets
# 6. Test build commands

# Uninstall when done testing
code --uninstall-extension TheAnsarya.poppy-assembly
```

---

## 🚀 Publishing

### 1. Login to Marketplace

```bash
# Login with Personal Access Token
vsce login TheAnsarya

# Enter your PAT when prompted
```

### 2. Publish Extension

```bash
# First publish (creates entry)
vsce publish

# Or specify version bump
vsce publish patch   # 0.1.0 → 0.1.1
vsce publish minor   # 0.1.0 → 0.2.0
vsce publish major   # 0.1.0 → 1.0.0

# Or publish specific version
vsce publish 1.0.0
```

### 3. Verify Publication

1. Visit <https://marketplace.visualstudio.com/manage/publishers/TheAnsarya>
2. Check extension appears in your list
3. Verify all metadata is correct
4. Test installation from marketplace:
   ```bash
   code --install-extension TheAnsarya.poppy-assembly
   ```

---

## 📝 Version Management

### Semantic Versioning
Follow semver (<https://semver.org/>):

- **MAJOR** (1.0.0): Breaking changes
- **MINOR** (0.1.0): New features (backward compatible)
- **PATCH** (0.0.1): Bug fixes (backward compatible)

### Update Checklist

1. Update version in `package.json`
2. Update `CHANGELOG.md` with changes
3. Create git tag for release:
   ```bash
   git tag -a v0.1.0 -m "Initial release"
   git push origin v0.1.0
   ```

### CHANGELOG.md Format
```markdown
# Changelog

## [0.2.0] - 2026-01-15
### Added
- New feature X
- New feature Y

### Changed
- Improved feature Z

### Fixed
- Bug fix for issue #123

## [0.1.0] - 2026-01-14
- Initial release
```

---

## 🔄 Update Process

### Publish Update

```bash
# 1. Make changes
yarn compile
yarn test

# 2. Update version and changelog
# Edit package.json (version)
# Edit CHANGELOG.md

# 3. Commit changes
git add .
git commit -m "Release v0.2.0: Add new features"
git tag v0.2.0

# 4. Package and publish
vsce publish

# 5. Push to GitHub
git push origin main --tags
```

---

## 🛠️ Troubleshooting

### "Missing publisher name"

- Ensure `publisher` field in package.json
- Must match your marketplace publisher ID

### "Invalid icon path"

- Remove icon field from package.json, or
- Add icon file and reference correct path

### "Missing LICENSE"

- Add LICENSE file, or
- Add `"license": "MIT"` to package.json

### "Package too large"

- Check .vscodeignore excludes node_modules
- Exclude test files and source .ts files
- Keep only compiled .js in package

### "Authentication failed"

- Regenerate Personal Access Token
- Ensure token has Marketplace → Manage scope
- Use `vsce logout` then `vsce login` again

---

## 📊 Analytics

### View Extension Stats

1. Go to <https://marketplace.visualstudio.com/manage/publishers/TheAnsarya>
2. Click on extension name
3. View:
   - Install count
   - Ratings and reviews
   - Acquisition funnel
   - Version adoption

---

## 🎯 Best Practices

### Before Publishing

- ✅ Test on clean VS Code install
- ✅ Test on Windows, Mac, Linux
- ✅ Run all tests
- ✅ Verify all links in README work
- ✅ Check screenshots are up to date
- ✅ Spell check all documentation

### Marketplace Listing

- Use clear, descriptive display name
- Write compelling description (first 200 chars matter)
- Add quality screenshots/GIFs
- Include feature badges
- Keep README concise but comprehensive
- Add "Getting Started" section
- Include contribution guidelines

### Post-Publication

- Monitor Q&A section for user questions
- Respond to reviews (good and bad)
- Track issues on GitHub
- Keep CHANGELOG updated
- Test beta features before releasing

---

## 📚 Resources

- [Publishing Extensions](https://code.visualstudio.com/api/working-with-extensions/publishing-extension)
- [Extension Manifest](https://code.visualstudio.com/api/references/extension-manifest)
- [Extension Capabilities](https://code.visualstudio.com/api/extension-capabilities/overview)
- [Extension Guidelines](https://code.visualstudio.com/api/references/extension-guidelines)
- [Marketplace](https://marketplace.visualstudio.com/)

---

## 🚦 Quick Reference

```bash
# Package
vsce package

# Test locally
code --install-extension *.vsix

# Publish patch update
vsce publish patch

# Unpublish (use with caution!)
vsce unpublish TheAnsarya.poppy-assembly

# Show package contents
vsce ls
```
