﻿using System;
using System.Collections.Generic;
using System.Configuration;
using NUnit.Framework;
using SolrNet;
using SolrNet.Exceptions;
using SolrNet.Impl;
using System.Reflection;
using StructureMap.SolrNetIntegration.Config;

namespace StructureMap.SolrNetIntegration.Tests
{
    [TestFixture]
    public class RegistryTests
    {

		private readonly string _solrUrl = ConfigurationManager.AppSettings["Solr.Baseline"];

        [Test]
        public void ResolveSolrOperations()
        {
            SetupContainer();
			var m = ObjectFactory.GetInstance<ISolrOperations<Entity>>();
            Assert.IsNotNull(m);            
        }

		[Ignore("Needs fixing")]
        [Test]
        public void RegistersSolrConnectionWithAppConfigServerUrl() {
            SetupContainer();
            var instanceKey = "entity" + typeof(SolrConnection);

            var solrConnection = (SolrConnection)ObjectFactory.Container.GetInstance<ISolrConnection>(instanceKey);

            Assert.AreEqual(_solrUrl+ "entity", solrConnection.ServerURL);
        }

		[Ignore("Needs fixing")]
        [Test, Category("Integration")]
        public void Ping_And_Query()
        {
            SetupContainer();
            var solr = ObjectFactory.GetInstance<ISolrOperations<Entity>>();
            solr.Ping();
            Console.WriteLine(solr.Query(SolrQuery.All).Count);
        }

        [Test, ExpectedException(typeof(InvalidURLException))]
        public void Should_throw_exception_for_invalid_protocol_on_url()
        {
            var solrServers = new SolrServers {
                new SolrServerElement {
                    Id = "test",
                    Url = "InvalidUrl",
                    DocumentType = typeof(Entity2).AssemblyQualifiedName,
                }                
            };
            ObjectFactory.Initialize(c => c.IncludeRegistry(new SolrNetRegistry(solrServers)));
            ObjectFactory.GetInstance<SolrConnection>();
        }

        [Test, ExpectedException(typeof(InvalidURLException))]
        public void Should_throw_exception_for_invalid_url()
        {
            var solrServers = new SolrServers {
                new SolrServerElement {
                    Id = "test",
                    Url = "InvalidUrl",
                    DocumentType = typeof(Entity2).AssemblyQualifiedName,
                }
            };
            ObjectFactory.Initialize(c => c.IncludeRegistry(new SolrNetRegistry(solrServers)));
            ObjectFactory.GetInstance<SolrConnection>();
        }

        [Test]
        public void Container_has_ISolrFieldParser()
        {
            SetupContainer();
            var parser = ObjectFactory.GetInstance<ISolrFieldParser>();
            Assert.IsNotNull(parser);
        }

        [Test]
        public void Container_has_ISolrFieldSerializer()
        {
           SetupContainer();
            ObjectFactory.GetInstance<ISolrFieldSerializer>();
        }

        [Test]
        public void Container_has_ISolrDocumentPropertyVisitor()
        {
            SetupContainer();
            ObjectFactory.GetInstance<ISolrDocumentPropertyVisitor>();
        }

        [Test]
        public void ResponseParsers()
        {
            SetupContainer();

            var parser = ObjectFactory.GetInstance<ISolrQueryResultParser<Entity>>() as SolrQueryResultParser<Entity>;

            var field = parser.GetType().GetField("parsers", BindingFlags.NonPublic | BindingFlags.Instance);
            var parsers = (ISolrResponseParser<Entity>[])field.GetValue(parser);
            Assert.AreEqual(11, parsers.Length);
            foreach (var t in parsers)
                Console.WriteLine(t);
        }

        [Test]
        public void DictionaryDocument_and_multi_core() {
            var cores = new SolrServers {
                new SolrServerElement {
                    Id = "default",
                    DocumentType = typeof(Entity).AssemblyQualifiedName,
                    Url = _solrUrl + "entity1",
                },
                new SolrServerElement {
                    Id = "entity1dict",
                    DocumentType = typeof(Dictionary<string, object>).AssemblyQualifiedName,
                    Url = _solrUrl + "entity1",
                },
                new SolrServerElement {
                    Id = "another",
                    DocumentType = typeof(Entity2).AssemblyQualifiedName,
                    Url = _solrUrl +"entity2",
                },
            };
            ObjectFactory.Initialize(c => c.IncludeRegistry(new SolrNetRegistry(cores)));
            var solr1 = ObjectFactory.GetInstance<ISolrOperations<Entity>>();
            var solr2 = ObjectFactory.GetInstance<ISolrOperations<Entity2>>();
            var solrDict = ObjectFactory.GetInstance<ISolrOperations<Dictionary<string, object>>>();
        }

		[Ignore ("Needs fixing")]
        [Test, Category("Integration")]
        public void DictionaryDocument()
        {
            SetupContainer();

            var solr = ObjectFactory.Container.GetInstance<ISolrOperations<Dictionary<string, object>>>();
            var results = solr.Query(SolrQuery.All);
            Assert.That(results.Count, Is.GreaterThan( 0));
            foreach (var d in results)
            {
				Assert.That(d.Count, Is.GreaterThan(0));
                foreach (var kv in d)
                    Console.WriteLine("{0}: {1}", kv.Key, kv.Value);
            }
        }

		[Ignore("Needs fixing")]
        [Test, Category("Integration")]
        public void DictionaryDocument_add()
        {
            SetupContainer();

            var solr = ObjectFactory.Container.GetInstance<ISolrOperations<Dictionary<string, object>>>();        

            solr.Add(new Dictionary<string, object> 
            {
                {"id", "ababa"},
                {"manu", "who knows"},
                {"popularity", 55},
                {"timestamp", DateTime.UtcNow},
            });
        }

        [Test]
        public void DictionaryDocument_ResponseParser()
        {
            SetupContainer();

            var parser = ObjectFactory.GetInstance<ISolrDocumentResponseParser<Dictionary<string, object>>>();
            Assert.That(parser, Is.InstanceOfType<SolrDictionaryDocumentResponseParser>());
        }

        [Test]
        public void DictionaryDocument_Serializer()
        {
            SetupContainer();
            var serializer = ObjectFactory.GetInstance<ISolrDocumentSerializer<Dictionary<string, object>>>();
            Assert.That(serializer, Is.InstanceOfType<SolrDictionarySerializer>());
        }

        private static void SetupContainer()
        {
            var solrConfig = (SolrConfigurationSection)ConfigurationManager.GetSection("solr");
            ObjectFactory.Initialize(c => c.IncludeRegistry(new SolrNetRegistry(solrConfig.SolrServers)));
        }

    }
}
