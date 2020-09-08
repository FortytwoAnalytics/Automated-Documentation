string basePath = System.AppDomain.CurrentDomain.BaseDirectory;
basePath =  System.IO.Path.Combine(basePath, "..","..","website");

// Create measures folder
var pathm = System.IO.Path.Combine(basePath, "docs");
System.IO.Directory.CreateDirectory(pathm);

pathm = System.IO.Path.Combine(pathm, "measures");
System.IO.Directory.CreateDirectory(pathm);

// Inline function to clean up content name for non alpanumeric characters
Func<string, string> cleanup = s => new String(Array.FindAll<char>(s.ToCharArray(), (c => (char.IsLetterOrDigit(c)
                                  || char.IsWhiteSpace(c)
                                  || c == '-'))));

foreach (var table in Model.Tables.Where(t => t.ObjectType.ToString() != "CalculationGroup" )) 
{

    string wikiPath = "/";
    
    var path = basePath + @"\docs" + wikiPath.Replace("/","\\") + table.Name.Replace(' ','-').Replace('/','-').Replace(@"\","-")  + ".md";
    
    // This text is added only once to the file.
    if (System.IO.File.Exists(path)) 
    {
        System.IO.File.Delete(path);
    }
    
    var navOrder = 1;
    
    // Create a file to write to.
    using (System.IO.StreamWriter sw = System.IO.File.CreateText(path)) 
    {
        navOrder++;
        sw.WriteLine("---");
        sw.WriteLine("layout: default");
        sw.WriteLine("title: "+ table.Name);
        sw.WriteLine("nav_order: "+ navOrder.ToString());
        sw.WriteLine("---");

        sw.WriteLine("# " + table.Name);
        sw.WriteLine("{:toc}");        
        sw.WriteLine("");
        sw.WriteLine(table.Description);
        sw.WriteLine("");
        sw.WriteLine("## Partition(s)");
        sw.WriteLine("Partitions which are part of this table. ");
        
        foreach (var partition in table.Partitions) {
            
            sw.WriteLine("### " + partition.Name); 
            sw.WriteLine("```sql");
            sw.WriteLine(partition.Query);
            sw.WriteLine("```");
         
            //System.Text.RegularExpressions.Regex expression = new System.Text.RegularExpressions.Regex(@"");
            //var results = expression.Matches(partition.Query);
            //
            //foreach (System.Text.RegularExpressions.Match match in results)
            //{
            //    sw.WriteLine(match.Groups["schema"].Value + " - " + match.Groups["object"].Value);
            //}
            
        }
        
        sw.WriteLine("");
        sw.WriteLine("## Data Columns");
        sw.WriteLine("");
        sw.WriteLine("| Columns | Data Type | Display Folder | Source Column | Relationship | Description |");
        sw.WriteLine("|--|--|--|--|:--:|--|");
   
        foreach (var column in table.DataColumns) {
            
            string colStart = (column.IsHidden ? "<font color=\"silver\">":"");
            string colEnd = (column.IsHidden ? "</font>":"");
            
            String tableRow = " | ";
 
            tableRow += colStart+column.Name + colEnd+" | ";
            tableRow += colStart+column.DataType + colEnd+" | ";
            tableRow += colStart+column.DisplayFolder +colEnd+" | ";
            tableRow += colStart+column.SourceColumn +colEnd+" | ";
            tableRow += colStart+(column.UsedInRelationships.Any() ? "X" : "") +colEnd+" | ";
            tableRow += colStart+column.Description +colEnd+" | ";
            
            sw.WriteLine(tableRow);
                
        }
        
        sw.WriteLine("");
        
        sw.WriteLine("## Relationships"); 
        sw.WriteLine("");      
        sw.WriteLine("| From | x | To |");
        sw.WriteLine("|--|--|--|--|:--:|--|");        
        
        foreach (var rel in table.UsedInRelationships) {
            
            String tableRow = "| ";
            
            if (rel.FromTable.Name == table.Name) {
            
                // From
                tableRow += "'" + rel.FromTable.Name + "'[" + rel.FromColumn.Name + "] | ";
                
                // Relationship
                tableRow += (rel.FromCardinality.ToString() == "Many" ? "(n)" : "(1)");
                tableRow += " ⟵ ";
                tableRow += (rel.ToCardinality.ToString() == "Many" ? "(n)" : "(1)");
                tableRow += " | ";
                
                // To
                tableRow += "['" + rel.ToTable.Name + "']({{ site.baseurl }}{% link docs/" + rel.ToTable.Name.Replace(" ","-") + ".md %})[" + rel.ToColumn.Name + "] | ";
                
            } else {
            
                // From
                tableRow += "'" + rel.ToTable.Name + "'[" + rel.ToColumn.Name + "] | ";                
                
                // Relationship
                tableRow += (rel.ToCardinality.ToString() == "Many" ? "(n)" : "(1)");
                tableRow += " ⟶ ";
                tableRow += (rel.FromCardinality.ToString()== "Many" ? "(n)" : "(1)");
                tableRow += " | ";
                
                // To
                tableRow += "['" + rel.FromTable.Name + "']({{ site.baseurl }}{% link docs/" + rel.FromTable.Name.Replace(" ","-") + ".md %})[" + rel.FromColumn.Name + "] |";
                
                
            }
            sw.WriteLine(tableRow);  
            
        }       

        sw.WriteLine("");        
        
        sw.WriteLine("## Relationship Diagram");
        
        sw.WriteLine("");      
        sw.WriteLine("<script src=\"https://cdn.jsdelivr.net/npm/mermaid@8.4.0/dist/mermaid.min.js\"></script>");
        sw.WriteLine("<script>mermaid.initialize({startOnLoad:true});</script>");
        sw.WriteLine("");  
        
        sw.WriteLine("<div class=\"mermaid\">");
        sw.WriteLine("graph LR;");
            
        foreach (var rel in table.UsedInRelationships.Where( r => !r.ToTable.Name.Contains("+") && !r.FromTable.Name.Contains("+") )) {
            
        String tableRow = "";
        // From
        tableRow += rel.FromTable.Name.Replace(" ","") + "(" + rel.FromTable.Name +") " ;
        tableRow += ( rel.IsActive ? "-->" : "-.->" );
        
        tableRow += " |\"";
        tableRow += (rel.FromCardinality.ToString()== "Many" ? "(n)" : "(1)");
        tableRow += " ⟶ ";
        tableRow += (rel.ToCardinality.ToString() == "Many" ? "(n)" : "(1)");
        tableRow += "\"| ";
        
        // To
        tableRow += rel.ToTable.Name.Replace(" ","");

        sw.WriteLine(tableRow+";");
                
        }       
  
        sw.WriteLine("</div>");        
        sw.WriteLine("");        
        sw.WriteLine("## Referenced by");        
        sw.WriteLine("");        
        sw.WriteLine("| Measure | Indrect dependencies |");
        sw.WriteLine("|--|--|");     
        
        foreach (var m in table.ReferencedBy.OfType<Measure>()) 
        {
            String tableRow = " | ";

            tableRow += "['" + m.Name + "']({{ site.baseurl }}{% link docs/measures/" + m.Name.Replace(" ","-").Replace(' ','-').Replace('/','-').Replace(@"\","-") + ".md %}) | ";
              
            var dep = m.ReferencedBy.Deep().OfType<Measure>().Count();
            
            
            tableRow += (dep > 0 ? dep.ToString() : "") +" | ";
            
            
            sw.WriteLine(tableRow);            
        }
    }   
    
}    


foreach (var m in Model.AllMeasures) 
{
   
    string wikiPath = "/";
    var path = basePath + @"\docs" + wikiPath.Replace("/","\\") + "measures/" + m.Name.Replace(' ','-').Replace('/','-').Replace(@"\","-").Replace("\"","") + ".md";
    // This text is added only once to the file.
    if (System.IO.File.Exists(path)) 
    {
        System.IO.File.Delete(path);
    }
    
    var navOrder = 0;
    // Create a file to write to.
    using (System.IO.StreamWriter sw = System.IO.File.CreateText(path)) 
    {
        
        navOrder++;
        sw.WriteLine("---");
        sw.WriteLine("layout: default");
        sw.WriteLine("title: "+ m.Name);
        sw.WriteLine("nav_order: "+ navOrder.ToString());
        sw.WriteLine("nav_exclude: true");
        //sw.WriteLine("parent: Key figures");
        sw.WriteLine("---");
        

        sw.WriteLine("# " + m.Name);
        sw.WriteLine("{:toc}");     
        sw.WriteLine("");  
        sw.WriteLine("## Descriptions");
        sw.WriteLine(m.Description);
        sw.WriteLine("");
        sw.WriteLine("## Expression");
        sw.WriteLine("```");
        sw.WriteLine(m.Expression);
        sw.WriteLine("```");
        
        sw.WriteLine("");    
        sw.WriteLine("## Depends on");
        
        // only insert tables if it exists
        if (m.DependsOn.OfType<Table>().Count() > 0) {
            
            sw.WriteLine("### Tables");
            sw.WriteLine(""); 
            sw.WriteLine("| Table | Column |");
            sw.WriteLine("|--|--|");     
            
            //var colDep = m.DependsOn.OfType<Measure>().GroupBy(x => x.Table.Name).Select(g => new
            //    {
            //        Name = g.Table.Name,
            //        Columns = String.Join(",", g.Select(x => x.Name))
            //    });
            //
            
            foreach (var mDep in m.DependsOn.OfType<Column>()) 
            {
                String tableRow = "| ";

                tableRow += "['" + mDep.Table.Name + "']({{ site.baseurl }}{% link docs/"+mDep.Table.Name.Replace(' ','-').Replace('/','-').Replace(@"\","-").Replace("\"","") + ".md %}) | ";
                tableRow += mDep.Name +" |";
                
                sw.WriteLine(tableRow);            
            }
        }
        
             
        // only insert measures if they exists
        if (m.DependsOn.OfType<Measure>().Count() > 0) {
            
            sw.WriteLine("");    
            sw.WriteLine("### Measures");
            sw.WriteLine(""); 
            sw.WriteLine("| Measure | Indrect dependencies |");
            sw.WriteLine("|--|--|");     
            
            foreach (var mRef in m.DependsOn.OfType<Measure>()) 
            {
                String tableRow = "| ";

                tableRow += "['" + mRef.Name + "']({{ site.baseurl }}{% link docs/measures/" + mRef.Name.Replace(' ','-').Replace('/','-').Replace(@"\","-").Replace("\"","") + ".md %})  | ";

                var dep = mRef.ReferencedBy.Deep().OfType<Measure>().Count();
                
                tableRow += (dep > 0 ? dep.ToString() : "") +" |";
                
                
                sw.WriteLine(tableRow);            
            }
        }        
        
        var refBy = m.ReferencedBy.OfType<Measure>();
        
        if (refBy.Count() > 0) {
            sw.WriteLine("");        
            sw.WriteLine("## Referenced by");        
            sw.WriteLine("");        
            sw.WriteLine("| Measure | Indrect dependencies |");
            sw.WriteLine("|--|--|");     
            
            foreach (var mRef in refBy) 
            {
                String tableRow = "| ";

                tableRow += "['" + mRef.Name + "']({{ site.baseurl }}{% link docs/measures/" + mRef.Name.Replace(' ','-').Replace('/','-').Replace(@"\","-").Replace("\"","") + ".md %}) | ";
                
                var dep = mRef.ReferencedBy.Deep().OfType<Measure>().Count();
                
                
                tableRow += (dep > 0 ? dep.ToString() : "") +" |";
                
                
                sw.WriteLine(tableRow);            
            }
        }
     
        
    }   
    
}   

var filePath =basePath + @"\docs\key-figures.md";
// This text is added only once to the file.
if (System.IO.File.Exists(filePath)) 
{
    System.IO.File.Delete(filePath);
}   

// Create a file to write to.
using (System.IO.StreamWriter sw = System.IO.File.CreateText(filePath)) 
{
    sw.WriteLine("---");
    sw.WriteLine("layout: default");
    sw.WriteLine("title: Key figures");
    sw.WriteLine("nav_order: 1");
    //sw.WriteLine("has_children: true");
    sw.WriteLine("search_exclude: true");
    sw.WriteLine("---");
    
    sw.WriteLine("");
    sw.WriteLine("Overview of key figures");
    sw.WriteLine("");
    sw.WriteLine(""); 
    sw.WriteLine("| Table | Column |");
    sw.WriteLine("|--|--|");    
    
    foreach (var m in Model.AllMeasures) 
    {

    var refCount = m.ReferencedBy.OfType<Measure>().Count();
    
    String tableRow = "| ";

    string colStart = (m.IsHidden ? "<font color=\"silver\">":"");
    string colEnd = (m.IsHidden ? "</font>":"");
            
    
    tableRow +=  "['" + m.Name + "']({{ site.baseurl }}{% link docs/measures/" + m.Name.Replace(' ','-').Replace('/','-').Replace(@"\","-").Replace("\"","") + ".md %})" +  " | ";
    tableRow +=  refCount.ToString()  +" |";
    
    sw.WriteLine(tableRow);
    } 
    
}  