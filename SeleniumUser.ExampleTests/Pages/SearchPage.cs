using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeleniumUser.ExampleTests.Pages
{
    public class SearchPage
    {
        public SearchPage Search()
        {
            User.Clicks(By.Name("btnG"));

            return new SearchPage();
        }

        public SearchPage SetSearchField(string text)
        {
            User.InputText(By.Name("q"), text);
            
            return new SearchPage();
        }

        public SearchPage ShouldSeeResult(string text)
        {
            User.ShouldSeeText(By.TagName("a"), text);

            return new SearchPage();
        }
    }
}
