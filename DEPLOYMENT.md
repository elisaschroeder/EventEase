# EventEase - Deployment Guide

EventEase is a modern Blazor WebAssembly application for event management, optimized for production deployment.

## 🚀 Quick Deployment

### Option 1: PowerShell Script (Recommended)
```powershell
.\deploy.ps1
```

### Option 2: Manual Build
```bash
dotnet publish --configuration Release --output ./publish
```

## 📦 Deployment Options

### 1. Static Web Hosting
**Best for**: GitHub Pages, Netlify, Vercel, Azure Static Web Apps

1. Run the deployment script or manual build
2. Upload contents of `./publish/wwwroot` to your hosting provider
3. Configure the hosting provider to serve `index.html` for all routes

**GitHub Pages Setup:**
- Enable GitHub Pages in repository settings
- Use the included GitHub Actions workflow (`.github/workflows/deploy.yml`)
- Push to main branch to trigger automatic deployment

### 2. Docker Container
**Best for**: Cloud platforms, self-hosted environments

```bash
# Build the Docker image
docker build -t eventease .

# Run the container
docker run -p 80:80 eventease
```

### 3. IIS Web Server
**Best for**: Windows servers, on-premises deployment

1. Install the ASP.NET Core Hosting Bundle on the server
2. Copy `./publish` contents to your IIS site directory
3. The included `web.config` handles routing and compression

### 4. Azure Static Web Apps
**Best for**: Azure cloud deployment

```bash
# Using Azure CLI
az staticwebapp create \
  --name eventease \
  --source ./publish/wwwroot \
  --location "Central US"
```

## 🔧 Configuration

### Performance Optimizations
- **Linking enabled**: Removes unused code in Release builds
- **Globalization optimized**: Reduces bundle size
- **Compression enabled**: Gzip compression for all assets
- **Caching configured**: Long-term caching for static assets

### Security Features
- **Security headers**: X-Frame-Options, X-Content-Type-Options, etc.
- **Content Security Policy**: Prevents XSS attacks
- **HTTPS redirection**: Enforced in production

### PWA Features
- **Web App Manifest**: Installable as a Progressive Web App
- **Service Worker ready**: Caching and offline support
- **Responsive design**: Works on all device sizes

## 📋 Pre-deployment Checklist

- [ ] Update app version in `EventEase.csproj`
- [ ] Test the application in Release mode locally
- [ ] Verify all environment-specific configurations
- [ ] Test routing and navigation
- [ ] Validate responsive design on different screen sizes
- [ ] Check performance with browser dev tools
- [ ] Verify all external dependencies are included

## 🌐 Domain Configuration

### Custom Domain Setup
1. Configure your DNS to point to your hosting provider
2. Update the `<base href="/" />` in `index.html` if serving from a subdirectory
3. Update the `start_url` in `manifest.json` accordingly

### HTTPS Setup
- Most hosting providers offer automatic HTTPS
- For custom servers, obtain an SSL certificate
- Configure redirect from HTTP to HTTPS

## 📊 Monitoring & Analytics

### Performance Monitoring
- Use browser dev tools to monitor load times
- Monitor bundle sizes after updates
- Set up application performance monitoring (APM)

### Analytics Integration
Add analytics scripts to `index.html`:
```html
<!-- Google Analytics -->
<script async src="https://www.googletagmanager.com/gtag/js?id=GA_MEASUREMENT_ID"></script>
<script>
  window.dataLayer = window.dataLayer || [];
  function gtag(){dataLayer.push(arguments);}
  gtag('js', new Date());
  gtag('config', 'GA_MEASUREMENT_ID');
</script>
```

## 🔧 Troubleshooting

### Common Issues

**404 Errors on Page Refresh**
- Ensure your hosting provider is configured to serve `index.html` for all routes
- Check that the `.nojekyll` file is present for GitHub Pages

**Large Bundle Size**
- Enable linking in Release mode (already configured)
- Consider using AOT compilation for smaller bundles
- Remove unused dependencies

**Slow Loading**
- Verify compression is enabled
- Check that static assets are cached properly
- Consider using a CDN for static assets

### Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build --configuration Release
```

## 📝 Maintenance

### Regular Updates
- Keep .NET and package versions updated
- Monitor security advisories
- Test deployments in staging environment
- Backup deployment artifacts

### Performance Optimization
- Monitor Core Web Vitals
- Optimize images and assets
- Update caching strategies
- Consider lazy loading for large components

## 🎯 Production Considerations

- **Environment Variables**: Use configuration providers for different environments
- **Logging**: Implement proper logging for production debugging
- **Error Handling**: Ensure graceful error handling and user feedback
- **Data Persistence**: Consider backend API integration for production data
- **User Authentication**: Implement proper authentication if required
- **Rate Limiting**: Add rate limiting for API calls
- **Content Delivery**: Use CDN for global content delivery

---

**Built with ❤️ using Blazor WebAssembly and .NET 9.0**