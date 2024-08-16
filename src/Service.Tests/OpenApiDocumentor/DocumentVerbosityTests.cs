// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.DataApiBuilder.Config.ObjectModel;
using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.DataApiBuilder.Service.Tests.OpenApiIntegration
{
    /// <summary>
    /// Integration tests validating that expected properties are present in the OpenApiDocument object.
    /// </summary>
    [TestCategory(TestCategory.MSSQL)]
    [TestClass]
    public class DocumentVerbosityTests
    {
        private const string CUSTOM_CONFIG = "doc-verbosity.MsSql.json";
        private const string MSSQL_ENVIRONMENT = TestCategory.MSSQL;
        private const string MISSING_TYPE_PROPERTY_ERROR = "Response object schema does not include a 'type' property.";
        private const string UNEXPECTED_CONTENTS_ERROR = "Unexpected number of response objects to validate.";

        /// <summary>
        /// Validates that for the Book entity, 7 response object schemas generated by OpenApiDocumentor
        /// contain a 'type' property with value 'object'.
        /// 
        /// Two paths:
        ///     - "/Books/id/{id}"
        ///         - 4 operations GET PUT PATCH DELETE
        ///             - Validate responses that return result contents:
        ///               GET (200), PUT (200, 201), PATCH (200, 201)
        ///     - "/Books"
        ///         - 2 operations GET(all) POST
        ///             - Validate responses that return result contents:
        ///               GET (200), POST (201)
        /// </summary>
        [TestMethod]
        public async Task ResponseObjectSchemaIncludesTypeProperty()
        {
            // Arrange
            Entity entity = new(
                Source: new(Object: "books", EntitySourceType.Table, null, null),
                GraphQL: new(Singular: null, Plural: null, Enabled: false),
                Rest: new(Methods: EntityRestOptions.DEFAULT_SUPPORTED_VERBS),
                Permissions: OpenApiTestBootstrap.CreateBasicPermissions(),
                Mappings: null,
                Relationships: null);

            Dictionary<string, Entity> entities = new()
            {
                { "Book", entity }
            };

            RuntimeEntities runtimeEntities = new(entities);

            // Act - Create OpenApi document
            OpenApiDocument openApiDocument = await OpenApiTestBootstrap.GenerateOpenApiDocumentAsync(
                runtimeEntities: runtimeEntities,
                configFileName: CUSTOM_CONFIG,
                databaseEnvironment: MSSQL_ENVIRONMENT);

            // Assert - Validate responses that return result contents: 200, 201
            List<OpenApiResponse> responses = openApiDocument.Paths.Values
                .SelectMany(pathObject => pathObject.Operations.Values)
                .SelectMany(operation => operation.Responses)
                // Responses Dictionary: Key: HttpStatusCode, Value: OpenApiResponse
                .Where(pair => pair.Key == "200" || pair.Key == "201")
                .Select(pair => pair.Value)
                .ToList();

            // Validate that 7 response object schemas contain a 'type' property with value 'object'
            // Test summary describes all 7 expected responses.
            Assert.IsTrue(
                condition: responses.Count == 7,
                message: UNEXPECTED_CONTENTS_ERROR);

            foreach (OpenApiResponse response in responses)
            {
                Assert.IsTrue(
                    condition: response.Content["application/json"].Schema.Type == "object",
                    message: MISSING_TYPE_PROPERTY_ERROR);
            }
        }
    }
}