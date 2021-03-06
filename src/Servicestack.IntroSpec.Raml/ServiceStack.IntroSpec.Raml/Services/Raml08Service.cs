﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace Servicestack.IntroSpec.Raml.Services
{
    using System.Linq;
    using DTO;
    using ServiceStack;
    using ServiceStack.IntroSpec.Raml.Extensions;
    using ServiceStack.IntroSpec.Services;
    using ServiceStack.IntroSpec.Extensions;

#if !DEBUG
    [CacheResponse(MaxAge = 300, Duration = 600)]
#endif
    public class Raml08Service : Service
    {
        private const string RamlVerison = "#%RAML 0.8";

        private readonly IApiDocumentationProvider documentationProvider;

        public Raml08Service(IApiDocumentationProvider documentationProvider)
        {
            documentationProvider.ThrowIfNull(nameof(documentationProvider));
            this.documentationProvider = documentationProvider;
        }

        [AddHeader(ContentType = Constants.RamlMediaType)]
        public object Get(RamlRequest request)
        {
            // Get the filtered documentation object
            // TODO - Make this an in-proc service call so that it can be overriden
            var documentation = documentationProvider.GetApiDocumentation(Request.GetApplicationUrl()).Filter(request);

            // Set RAML version - picked up by renderer
            Request.SetRamlVersion(RamlVerison);

            // Convert IntroSpec object to RAML
            var allowedFormats =
                HostContext.MetadataPagesConfig.AvailableFormatConfigs.Select(a => $".{a.Format}").ToHashSet();
            var generator = new RamlCollectionGenerator(allowedFormats);
            var raml = generator.Generate(documentation);

            return raml;
        }
    }
}
