using System;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Store;
//using static Lucene.Net.Analysis.Shingle.ShingleFilter;

using Lucene.Net.Analysis.Standard;
//using static Lucene.Net.Analysis.TokenStream;
using Lucene.Net.Util;
using System.Diagnostics;
using LuceneDirectory = Lucene.Net.Store.Directory;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Analysis.Ru;
using Lucene.Net.Analysis.En;
using Lucene.Net.Search.Similarities;
//using Lucene.Net.Search.Spell;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Lucene.Net.Analysis.Shingle;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Search.Spans;
using Lucene.Net.QueryParsers.ComplexPhrase;
//using static Lucene.Net.Search.SimpleFacetedSearch;
//using Hits = Lucene.Net.Search.Hits;
//< PackageReference Include = "Lucene.Net.Contrib" Version = "3.0.3" />
//cd C:\Users\lahas\Downloads\lucene - example1
namespace lucene_prog
{
    class Program
    {
        static Analyzer analyzer;
        const LuceneVersion luceneVersion = LuceneVersion.LUCENE_48;
        public class Article
        {
            public string ID { get; set; }
            public string title { get; set; }
            public string text { get; set; }
            public string date { get; set; }
        }
        public static Query createPhraseQuery(String[] phraseWords, String field)
        {
            SpanQuery[] queryParts = new SpanQuery[phraseWords.Length];
            for (int i = 0; i < phraseWords.Length; i++)
            {
                WildcardQuery wildQuery = new WildcardQuery(new Term(field, phraseWords[i]));
                queryParts[i] = new SpanMultiTermQueryWrapper<WildcardQuery>(wildQuery);
            }
            return new SpanNearQuery(queryParts,       //words
                                     0,                //max distance
                                     true              //exact order
            );
        }



        public class NGramsAnalyzer : Analyzer
        {

            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {

                //return new ShingleFilter(tokenizedInput, 4);



                var tokenizer = new StandardTokenizer(LuceneVersion.LUCENE_48, reader);
                var tokenizedInput = new LowerCaseFilter(luceneVersion, new StandardFilter(luceneVersion, tokenizer));
                var output = new ShingleFilter(tokenizedInput, 2);
                //var lowerCaseFilter = new LowerCaseFilter(luceneVersion, shingleMatrix);
                //var shingleMatrix = new ShingleFilter(tokenizer, 2);
                var result = new StopFilter(luceneVersion, output, StopAnalyzer.ENGLISH_STOP_WORDS_SET);

                //Tokenizer tokenizer = new StandardTokenizer(Version.LUCENE_48, reader);
                //TokenStream result = new SynonymFilter(tokenizer, smap, true);
                return new TokenStreamComponents(tokenizer, result);


            }
        }


        static void Main(string[] args)
        {


            // Specify the compatibility version we want


            //Open the Directory using a Lucene Directory class
            string indexName = "example_index";
            string indexPath = Path.Combine(Environment.CurrentDirectory, indexName);

            using LuceneDirectory indexDir = FSDirectory.Open(indexPath);

            //Create an analyzer to process the text 
            //Analyzer analyzer = new StandardAnalyzer(luceneVersion);
            //Analyzer analyzer = new RussianAnalyzer(luceneVersion);
            //Analyzer analyzer = new WhitespaceAnalyzer(luceneVersion); //NGramAnalyzer();
            Analyzer analyzer = new NGramsAnalyzer();

            //Create an index writer
            IndexWriterConfig indexConfig = new IndexWriterConfig(luceneVersion, analyzer);
            indexConfig.OpenMode = OpenMode.CREATE;                             // create/overwrite index
            IndexWriter writer = new IndexWriter(indexDir, indexConfig);

            //Add three documents to the index
            Document doc = new Document();
            for (int i = 1; i < 1396; i++)
            {
                string path = @"C:/Users/lahas/Downloads/search_doc/result" + i + ".json";
                string data = File.ReadAllText(path);
                Article person = JsonSerializer.Deserialize<Article>(data);

                doc = new Document();
                doc.Add(new StringField("id", person.ID, Field.Store.YES));
                doc.Add(new Field("title", person.title, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
                doc.Add(new Field("text", person.text, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
                //, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS
                doc.Add(new StringField("date", person.date, Field.Store.YES));
                writer.AddDocument(doc);
            }
            //Flush and commit the index data to the directory
            writer.Commit();
            //TokenStream stream = analyzer.TokenStream("content", new StringReader(str));
            //Token token = new Token();
            //while ((token = stream.next(token)) != null)
            //{
            //    System.out.println(token.term());
            //}
            using DirectoryReader reader = writer.GetReader(applyAllDeletes: true);
            IndexSearcher searcher = new IndexSearcher(reader);
            QueryParser parser = new MultiFieldQueryParser(luceneVersion, new string[] { "title", "text" }, analyzer);
            ////фраза только целеком и в нужной форме чере  standart analyzer
            //PhraseQuery pq = new PhraseQuery();
            //pq.Add(new Term("text", "язык"));
            //pq.Add(new Term("text", "программирования"));

            //TopDocs td = searcher.Search(pq, 10);

            ////фраза только целеком и в нужной форме чере n grams
            //PhraseQuery pq = new PhraseQuery();
            //pq.Add(new Term("text", "языка программирования"));
            //TopDocs td = searcher.Search(pq, 10);

            //("\"языка программирования\"~3");

            //Query query = parser.Parse("языка программирования");
            //TopDocs td = searcher.Search(query, n: 1396);

            ////через стандартный анализатор поиска фразы с помарками в форме
            //String[] phraseWords = new String[] { "язык*", "программирования" };
            //Query phraseQuery = createPhraseQuery(phraseWords, "text");
            //TopDocs td = searcher.Search(phraseQuery, 10);

            //нечёткий поиск для n-gram(2)
            Query query = new FuzzyQuery(new Term("text", "Языки программирования"), 2);
            TopDocs td = searcher.Search(query, n: 1396);
            Console.WriteLine($"Matching results: {td.TotalHits}");


            for (int i = 0; i < td.TotalHits; i++)
            {
                //read back a doc from results
                Document resultDoc = searcher.Doc(td.ScoreDocs[i].Doc);

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

