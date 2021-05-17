﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using M11MVC.Models;
//using System.Web.Mvc;

namespace M11MVC.Controllers
{
  [RoutePrefix("prod")]
  public class ProductController : ApiController
  {
    Product[] products = new Product[]
       {
              new Product { Id = 1, Name = "Tomato Soup1111", Category = "Groceries", Price = 1 },
              new Product { Id = 2, Name = "Yo-yo", Category = "Toys", Price = 3.75M },
              new Product { Id = 3, Name = "Hammer", Category = "Hardware", Price = 16.99M }
       };

    [HttpGet]
    [Route("product")]
    public IEnumerable<Product> GetAllProducts()
    {


      return products;
    }

    
  }
}
