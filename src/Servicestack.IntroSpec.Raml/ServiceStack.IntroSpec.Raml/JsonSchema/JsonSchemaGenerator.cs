﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.IntroSpec.Raml.JsonSchema
{
    using System.Collections.Generic;
    using System.Linq;
    using Extensions;
    using IntroSpec.Extensions;
    using IntroSpec.Models;

    public static class JsonSchemaGenerator
    {
        public static JsonSchema Generate(IApiResourceType resource)
        {
            // Create with default schema
            var schema = new JsonSchema
            {
                Description = $"Schema for {resource.Title}. {resource.Description}",
                Title = resource.Title,
                Type = JsonSchemaTypeLookup.GetJsonType(resource.TypeName)
            };

            if (resource.Properties.IsNullOrEmpty())
                return schema;

            schema.Definitions = GetDefinitions(resource);
            PopulateBaseFields(schema, resource.Properties);

            return schema;
        }

        public static Dictionary<string, JsonDefinition> GetDefinitions(IApiResourceType resource)
        {
            // Get all embedded refs and convert to definitions
            var propertiesWithResources = resource.Properties.SelectMany(GetPropertiesWithEmbeddedResources).ToList();
            
            var dictionary = new Dictionary<string, JsonDefinition>();
            if (propertiesWithResources.IsNullOrEmpty())
                return dictionary;

            foreach (var prop in propertiesWithResources)
            {
                dictionary.SafeAdd(prop.EmbeddedResource.TypeName, prop.ConvertToDefinition());
            }

            return dictionary;
        }

        public static JsonDefinition ConvertToDefinition(this ApiPropertyDocumention property)
        {
            var definition = new JsonDefinition();
            definition.Type = JsonSchemaTypeLookup.GetJsonTypes(property.ClrType, property.IsRequired ?? false);

            // Required is every prop that IsRequired = true
            PopulateBaseFields(definition, property.EmbeddedResource.Properties);

            return definition;
        }

        public static IEnumerable<ApiPropertyDocumention> GetPropertiesWithEmbeddedResources(ApiPropertyDocumention prop)
        {
            if (prop.EmbeddedResource != null)
            {
                yield return prop;

                if (!prop.EmbeddedResource.Properties.IsNullOrEmpty())
                    foreach (var p in prop.EmbeddedResource.Properties.SelectMany(GetPropertiesWithEmbeddedResources))
                        yield return p;
            }
        }

        public static void PopulateBaseFields(IJsonSchemaBase obj, IEnumerable<ApiPropertyDocumention> properties)
        {
            var dict = new Dictionary<string, JsonProperty>();
            var apiPropertyDocumentions = properties as ApiPropertyDocumention[] ?? properties.ToArray();

            var requiredList = new List<string>(apiPropertyDocumentions.Length);

            foreach (var property in apiPropertyDocumentions)
            {
                var jsonProp = new JsonProperty
                {
                    Type = JsonSchemaTypeLookup.GetJsonTypes(property.ClrType, property.IsRequired ?? false)
                };

                if (property.IsRequired ?? false)
                    requiredList.Add(property.Id);

                if (property.EmbeddedResource != null)
                    jsonProp.AddRef(property.EmbeddedResource.Title);

                dict.Add(property.Id, jsonProp);
            }

            obj.Properties = dict.IsNullOrEmpty() ? null : dict;
            obj.Required = requiredList.IsNullOrEmpty() ? null : requiredList;
        }
    }
}
