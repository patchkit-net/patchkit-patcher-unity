using UnityEngine;
using System.Collections;

namespace PatchKit.Unity.Common
{
    public class NoInternetConnectionException : System.Exception 
    {
        public NoInternetConnectionException() : base("No internet connection.")
        {
        }
    }
}