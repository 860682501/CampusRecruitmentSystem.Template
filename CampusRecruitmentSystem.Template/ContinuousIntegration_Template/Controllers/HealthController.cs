﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ContinuousIntegration_Template.Controllers
{
    [Route("api/[controller]")]
    public class HealthController : Controller
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Ok");
        }
    }
}
