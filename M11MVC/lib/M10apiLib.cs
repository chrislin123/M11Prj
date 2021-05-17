using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace M11MVC.lib
{
  public class M11MVCLib
  {

    public static System.Collections.Specialized.NameValueCollection ParseQueryString(string QueryString)
    { 
      System.Collections.Specialized.NameValueCollection result = new System.Collections.Specialized.NameValueCollection();
      result =  HttpUtility.ParseQueryString(QueryString);
      return result;
    }

  }
}