// --- System & IO ---
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using System.IO;               // Fixes Directory, Path, FileStream

// --- ASP.NET Core ---
global using Microsoft.AspNetCore.Builder; // Fixes WebApplication
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Routing;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection; // Fixes GetRequiredService
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;

// --- MVC ---
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.Rendering;
global using Microsoft.AspNetCore.Mvc.ViewFeatures;

// --- Project Namespaces ---
global using NatureBasketBoutique.Models;
global using NatureBasketBoutique.Data;
global using NatureBasketBoutique.Repository.IRepository;
global using NatureBasketBoutique.ViewModels;