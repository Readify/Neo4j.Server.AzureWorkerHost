﻿using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using Neo4j.Server.AzureWorkerHost;
using Xunit;
using Xunit.Extensions;

namespace Tests.NeoServerTests
{
    public class ApplyEndpointConfigurationTests
    {
        [Theory]
        [InlineData("#org.neo4j.server.webserver.address=0.0.0.0", "org.neo4j.server.webserver.address=1.2.3.4")]
        [InlineData("org.neo4j.server.webserver.port=7474", "org.neo4j.server.webserver.port=5678")]
        [InlineData("org.neo4j.server.webserver.https.enabled=true", "org.neo4j.server.webserver.https.enabled=false")]
        [InlineData(OriginalConfigFileContents, PatchedConfigFileContents)]
        public void ShouldPatchIpAndPortAndSsl(string input, string expected)
        {
            // Arrange
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"c:\temp\neo4j\neo-v1234\conf\neo4j-server.properties", new MockFileData(input) }
            });
            var server = new NeoServer(new NeoServerConfiguration(), null, null, fileSystem, null);
            server.Context.NeoEndpoint = new IPEndPoint(IPAddress.Parse("1.2.3.4"), 5678);
            server.Context.NeoBasePath = @"c:\temp\neo4j\neo-v1234\";

            // Act
            server.ApplyEndpointConfiguration();

            // Assert
            var content = fileSystem.File.ReadAllText(@"c:\temp\neo4j\neo-v1234\conf\neo4j-server.properties");
            Assert.Equal(expected, content);
        }

        const string OriginalConfigFileContents = @"################################################################
# Neo4j configuration
#
################################################################

#***************************************************************
# Server configuration
#***************************************************************

# location of the database directory 
org.neo4j.server.database.location=data/graph.db

# let the webserver only listen on the specified IP. Default
# is localhost (only accept local connections). Uncomment to allow
# any connection. Please see the security section in the neo4j 
# manual before modifying this.
#org.neo4j.server.webserver.address=0.0.0.0

#
# HTTP Connector
#

# http port (for all data, administrative, and UI access)
org.neo4j.server.webserver.port=7474

#
# HTTPS Connector
#

# Turn https-support on/off
org.neo4j.server.webserver.https.enabled=true

# https port (for all data, administrative, and UI access)
org.neo4j.server.webserver.https.port=7473

# Certificate location (auto generated if the file does not exist)
org.neo4j.server.webserver.https.cert.location=conf/ssl/snakeoil.cert

# Private key location (auto generated if the file does not exist)
org.neo4j.server.webserver.https.key.location=conf/ssl/snakeoil.key

# Internally generated keystore (don't try to put your own
# keystore there, it will get deleted when the server starts)
org.neo4j.server.webserver.https.keystore.location=data/keystore

#*****************************************************************
# Administration client configuration
#*****************************************************************

# location of the servers round-robin database directory. possible values:
# - absolute path like /var/rrd
# - path relative to the server working directory like data/rrd
# - commented out, will default to the database data directory.
org.neo4j.server.webadmin.rrdb.location=data/rrd

# REST endpoint for the data API
# Note the / in the end is mandatory
org.neo4j.server.webadmin.data.uri=/db/data/

# REST endpoint of the administration API (used by Webadmin)
org.neo4j.server.webadmin.management.uri=/db/manage/

# Low-level graph engine tuning file
org.neo4j.server.db.tuning.properties=conf/neo4j.properties


# Comma separated list of JAX-RS packages containing JAX-RS resources, one package name for each mountpoint.
# The listed package names will be loaded under the mountpoints specified. Uncomment this line
# to mount the org.neo4j.examples.server.unmanaged.HelloWorldResource.java from neo4j-examples
# under /examples/unmanaged, resulting in a final URL of
# http://localhost:7474/examples/unmanaged/helloworld/{nodeId}
#org.neo4j.server.thirdparty_jaxrs_classes=org.neo4j.examples.server.unmanaged=/examples/unmanaged


#*****************************************************************
# HTTP logging configuration
#*****************************************************************

# HTTP logging is disabled. HTTP logging can be enabled by setting this property to 'true'.
org.neo4j.server.http.log.enabled=false 

# Logging policy file that governs how HTTP log output is presented and archived.
# Note: changing the rollover and retention policy is sensible, but changing the
# output format is less so, since it is configured to use the ubiquitous common log format
org.neo4j.server.http.log.config=conf/neo4j-http-logging.xml
";

        const string PatchedConfigFileContents = @"################################################################
# Neo4j configuration
#
################################################################

#***************************************************************
# Server configuration
#***************************************************************

# location of the database directory 
org.neo4j.server.database.location=data/graph.db

# let the webserver only listen on the specified IP. Default
# is localhost (only accept local connections). Uncomment to allow
# any connection. Please see the security section in the neo4j 
# manual before modifying this.
org.neo4j.server.webserver.address=1.2.3.4

#
# HTTP Connector
#

# http port (for all data, administrative, and UI access)
org.neo4j.server.webserver.port=5678

#
# HTTPS Connector
#

# Turn https-support on/off
org.neo4j.server.webserver.https.enabled=false

# https port (for all data, administrative, and UI access)
org.neo4j.server.webserver.https.port=7473

# Certificate location (auto generated if the file does not exist)
org.neo4j.server.webserver.https.cert.location=conf/ssl/snakeoil.cert

# Private key location (auto generated if the file does not exist)
org.neo4j.server.webserver.https.key.location=conf/ssl/snakeoil.key

# Internally generated keystore (don't try to put your own
# keystore there, it will get deleted when the server starts)
org.neo4j.server.webserver.https.keystore.location=data/keystore

#*****************************************************************
# Administration client configuration
#*****************************************************************

# location of the servers round-robin database directory. possible values:
# - absolute path like /var/rrd
# - path relative to the server working directory like data/rrd
# - commented out, will default to the database data directory.
org.neo4j.server.webadmin.rrdb.location=data/rrd

# REST endpoint for the data API
# Note the / in the end is mandatory
org.neo4j.server.webadmin.data.uri=/db/data/

# REST endpoint of the administration API (used by Webadmin)
org.neo4j.server.webadmin.management.uri=/db/manage/

# Low-level graph engine tuning file
org.neo4j.server.db.tuning.properties=conf/neo4j.properties


# Comma separated list of JAX-RS packages containing JAX-RS resources, one package name for each mountpoint.
# The listed package names will be loaded under the mountpoints specified. Uncomment this line
# to mount the org.neo4j.examples.server.unmanaged.HelloWorldResource.java from neo4j-examples
# under /examples/unmanaged, resulting in a final URL of
# http://localhost:7474/examples/unmanaged/helloworld/{nodeId}
#org.neo4j.server.thirdparty_jaxrs_classes=org.neo4j.examples.server.unmanaged=/examples/unmanaged


#*****************************************************************
# HTTP logging configuration
#*****************************************************************

# HTTP logging is disabled. HTTP logging can be enabled by setting this property to 'true'.
org.neo4j.server.http.log.enabled=false 

# Logging policy file that governs how HTTP log output is presented and archived.
# Note: changing the rollover and retention policy is sensible, but changing the
# output format is less so, since it is configured to use the ubiquitous common log format
org.neo4j.server.http.log.config=conf/neo4j-http-logging.xml
";
    }
}
