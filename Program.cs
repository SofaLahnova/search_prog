using System;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System.Diagnostics;
using LuceneDirectory = Lucene.Net.Store.Directory;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;


namespace lucene
{
    class Program
    {
         public class Article
        {
        public string ID { get; set; }
        public string title { get; set; }
        public string text { get; set; }
        public string date { get; set; }
         }
        static void Main(string[] args)
         {


        // Specify the compatibility version we want
        const LuceneVersion luceneVersion = LuceneVersion.LUCENE_48;

        //Open the Directory using a Lucene Directory class
        string indexName = "example_index";
        string indexPath = Path.Combine(Environment.CurrentDirectory, indexName);

        using LuceneDirectory indexDir = FSDirectory.Open(indexPath);

        //Create an analyzer to process the text 
        Analyzer standardAnalyzer = new StandardAnalyzer(luceneVersion);

        //Create an index writer
        IndexWriterConfig indexConfig = new IndexWriterConfig(luceneVersion, standardAnalyzer);
        indexConfig.OpenMode = OpenMode.CREATE;                             // create/overwrite index
        IndexWriter writer = new IndexWriter(indexDir, indexConfig);

        //Add three documents to the index
        Document doc;
            for (int i = 1; i < 1396; i++)
            {
                string path = @"C:\Users\lahas\Downloads\search_doc\result" + i + ".json";
                string data = File.ReadAllText(path);
                Article person = JsonSerializer.Deserialize<Article>(data);

                doc = new Document();
                doc.Add(new StringField("id", person.ID, Field.Store.YES));
                doc.Add(new TextField("title", person.title, Field.Store.YES));
                doc.Add(new TextField("text", person.text, Field.Store.YES));
                doc.Add(new StringField("date", person.date, Field.Store.YES));
                writer.AddDocument(doc);
            }
        //Flush and commit the index data to the directory
        writer.Commit();

        using DirectoryReader reader = writer.GetReader(applyAllDeletes: true);
        IndexSearcher searcher = new IndexSearcher(reader);
            QueryParser parser = new MultiFieldQueryParser(luceneVersion, new string[] { "title", "text" }, standardAnalyzer);
        Query query = parser.Parse("Neuralink");
        TopDocs topDocs = searcher.Search(query, n: 1396);         //indicate we want the first 3 results


        Console.WriteLine($"Matching results: {topDocs.TotalHits}");

        for (int i = 0; i < topDocs.TotalHits; i++)
        {
            //read back a doc from results
            Document resultDoc = searcher.Doc(topDocs.ScoreDocs[i].Doc);

            string title = resultDoc.Get("title");
            string id0 = resultDoc.Get("id");
            string text = resultDoc.Get("text");
            Console.WriteLine($"ID of result {i + 1}: {id0}");
            Console.WriteLine($"Title of result {i + 1}: {title}");
            Console.WriteLine($"Text of result {i + 1}: {text}");
        }
    }
} 
}
