using CatalogService.Api.Core.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace CatalogService.Api.Infrastructure.Context
{
    public class CatalogContextSeed
    {
        public async Task SeedAsync(CatalogContext context, IWebHostEnvironment env, ILogger<CatalogContextSeed> logger)
        {
            var policy = Policy.Handle<SqlException>().
                WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retry => TimeSpan.FromSeconds(5),
                onRetry: (ex, timespan, retry, ctx) =>
                {
                    logger.LogWarning(ex, "[{prefix}] Exception {ExceptionType} with message {Message} detected on attempt {retry} of");
                });

            var setupDirPath = Path.Combine(env.ContentRootPath, "Infrastructure", "Setup", "SeedFiles");
            var picturePath = "Pics";

            await policy.ExecuteAsync(() => ProcessSeeding(context, setupDirPath, picturePath, logger));
        }

        private async Task ProcessSeeding(CatalogContext context,string setupDirPath,string picturePath,ILogger logger)
        {
            if (!context.CatalogBrands.Any())
            {
                await context.CatalogBrands.AddRangeAsync(GetCatalogBrandsFromFile(setupDirPath));
                await context.SaveChangesAsync();
            }
            if (!context.CatalogTypes.Any())
            {
                await context.CatalogTypes.AddRangeAsync(GetCatalogTypesFromFile(setupDirPath));
                await context.SaveChangesAsync();
            } 
            if (!context.CatalogItems.Any())
            {
                await context.CatalogItems.AddRangeAsync(GetCatalogItemsFromFile(setupDirPath,context));
                await context.SaveChangesAsync();

                GetCatalogItemPictures(setupDirPath, picturePath);
            }
        }
        private IEnumerable<CatalogBrand> GetCatalogBrandsFromFile(string contentPath)
        {
            IEnumerable<CatalogBrand> GetPreConfiguredCatalogBrands()
            {
                return new List<CatalogBrand>()
                {
                    new CatalogBrand{Brand="Azure"},
                    new CatalogBrand{Brand=".NET"},
                    new CatalogBrand{Brand="Visual Studio"},
                    new CatalogBrand{Brand="SQL Server"},
                    new CatalogBrand{Brand="Other"},
                    new CatalogBrand{Brand="CatalogBrandTestOne"},
                    new CatalogBrand{Brand="CatalogBrandTestTwo"},
                   
                };
            }
            string fileName = Path.Combine(contentPath, "BrandsTextFile.txt");
            if (!File.Exists(fileName))
            {
                return GetPreConfiguredCatalogBrands();
            }
            var fileContent = File.ReadAllLines(fileName);
            var list = fileContent.Select(x => new CatalogBrand()
            {
                Brand = x.Trim('"')
            }).Where(i => i != null);

            return list ?? GetPreConfiguredCatalogBrands();
        } 
        private IEnumerable<CatalogType> GetCatalogTypesFromFile(string contentPath)
        {
            IEnumerable<CatalogType> GetPreConfiguredCatalogTypes()
            {
                return new List<CatalogType>()
                {
                    new CatalogType{Type="Mug"},
                    new CatalogType{Type="T-Shirt"},
                    new CatalogType{Type="Sheet"},
                    new CatalogType{Type="USB Memory Stick"},
                    new CatalogType{Type="CatalogTypeTestOne"},
                    new CatalogType{Type="CatalogTypeTestTwo"},
                };
            }
            string fileName = Path.Combine(contentPath, "CatalogTypes.txt");
            if (!File.Exists(fileName))
            {
                return GetPreConfiguredCatalogTypes();
            }
            var fileContent = File.ReadAllLines(fileName);
            var list=fileContent.Select(x => new CatalogType()
            { 
                Type = x.Trim('"') 
            }).Where(i=>i!=null);

            return list?? GetPreConfiguredCatalogTypes();
        }
        private IEnumerable<CatalogItem> GetCatalogItemsFromFile(string contentPath, CatalogContext context)
        {
            IEnumerable<CatalogItem> GetPreConfiguredItems()
            {
                return new List<CatalogItem>()
                {
                    new CatalogItem{CatalogTypeId=2,CatalogBrandId=2,AvailableStock=100,Description=".NET Bot Black Hoodie", Name=".NET Bot Black Hoodie"},
                    new CatalogItem{CatalogTypeId=1,CatalogBrandId=2,AvailableStock=100,Description=".NET Black & White Mug", Name=".NET Black & White Mug"},
                    new CatalogItem{CatalogTypeId=2,CatalogBrandId=5,AvailableStock=100,Description="Prism White T-Shirt", Name="Prism White T-Shirt"},
                    new CatalogItem{CatalogTypeId=2,CatalogBrandId=2,AvailableStock=100,Description=".NET Foundation T-shirt", Name=".NET Foundation T-shirt"},
                    new CatalogItem{CatalogTypeId=3,CatalogBrandId=5,AvailableStock=100,Description="Roslyn Red Sheet", Name="Roslyn Red Sheet"},
                    new CatalogItem{CatalogTypeId=2,CatalogBrandId=2,AvailableStock=100,Description=".NET Blue Hoodie", Name=".NET Blue Hoodie"},
                    new CatalogItem{CatalogTypeId=2,CatalogBrandId=5,AvailableStock=100,Description="Roslyn Red T-Shirt", Name="Roslyn Red T-Shirt"},
                    new CatalogItem{CatalogTypeId=2,CatalogBrandId=5,AvailableStock=100,Description="Kudu Purple Hoodie", Name="Kudu Purple Hoodie"},
                    new CatalogItem{CatalogTypeId=1,CatalogBrandId=5,AvailableStock=100,Description="Cup<T> White Mug", Name="Cup<T> White Mug"},
                    new CatalogItem{CatalogTypeId=3,CatalogBrandId=2,AvailableStock=100,Description=".NET Foundation Sheet", Name=".NET Foundation Sheet"},
                    new CatalogItem{CatalogTypeId=3,CatalogBrandId=2,AvailableStock=100,Description="Cup<T> Sheet", Name="Cup<T> Sheet"},
                    new CatalogItem{CatalogTypeId=2,CatalogBrandId=5,AvailableStock=100,Description="Prism White TShirt", Name="Prism White TShirt"},
                    new CatalogItem{CatalogTypeId=1,CatalogBrandId=5,AvailableStock=100,Description="De los Palotes", Name="Pepito"},
                };
            }
            string fileName = Path.Combine(contentPath, "CatalogItems.txt");
            if (!File.Exists(fileName))
            {
                return GetPreConfiguredItems();
            }
            var catalogTypeIdLookUp = context.CatalogTypes.ToDictionary(ct => ct.Type, ct => ct.Id);
            var catalogBrandIdLookUp = context.CatalogBrands.ToDictionary(cb => cb.Brand, cb => cb.Id);

            var fileContent = File.ReadAllLines(fileName)
                .Skip(2)
                .Select(i => i.Split(','))
                .Select(i => new CatalogItem()
                {
                    CatalogTypeId = catalogTypeIdLookUp[i[0]],
                    CatalogBrandId = catalogBrandIdLookUp[i[1]],
                    Description= i[2].Trim('"').Trim(),
                    Name= i[3].Trim('"').Trim(),
                    Price = Decimal.Parse(i[4].Trim(), NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture),
                    PictureFileName= i[5].Trim('"').Trim(),
                    AvailableStock = string.IsNullOrEmpty(i[6]) ? 0 : int.Parse(i[6]),
                    OnReorder = Convert.ToBoolean(i[7])
                });
            return fileContent;
        }
        private void GetCatalogItemPictures(string contentPath, string picturePath)
        {
            picturePath ??= "Pics";
            if (picturePath != null)
            {
                DirectoryInfo directory = new DirectoryInfo(picturePath);
                foreach (FileInfo file in directory.GetFiles())
                {
                    file.Delete();
                }
                string zipFileCatalogItemPictures = Path.Combine(contentPath, "CatalogItems.zip");
                ZipFile.ExtractToDirectory(zipFileCatalogItemPictures, picturePath);
            }
        }
    }
}
