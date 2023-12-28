- Feature Name: Add OpenTelemetry Tracing
- Start Date: 2023-12-27

# Summary
[summary]: #summary

[OpenTelemetry](https://opentelemetry.io/docs/what-is-opentelemetry/) is a collection of APIs, SDKs, and tools used to instrument, generate, collect, and export telemetry data (metrics, logs, and traces) to help the analysis of software’s performance and behavior.
This document describes the necessary steps to include OpenTelemetry tracing in C# driver.

# Motivation
[motivation]: #motivation

OpenTelemetry is the industry standard regarding telemetry data that aggregates logs, metrics, and traces. Specifically regarding traces, it allows the developers to understand the full "path" a request takes in the application and navigate through a microservice architecture.
For the .NET ecosystem, there are available implementations regarding the export of telemetry data in client-side calls in some major database management systems, being them community lead efforts ([SqlClient](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.SqlClient), [MongoDB](https://github.com/jbogard/MongoDB.Driver.Core.Extensions.OpenTelemetry)), or native implementations ([Elasticsearch](https://github.com/elastic/elasticsearch-net/blob/main/src/Elastic.Clients.Elasticsearch/Client/ElasticsearchClient.cs#L183)).

The inclusion of telemetry in the Cassandra C# driver allows the developers to have access to Cassandra operations when analyzing the requests that are handled in their systems when it includes Cassandra calls.

# Guide-level explanation
[guide-level-explanation]: #guide-level-explanation

## [Traces](https://opentelemetry.io/docs/concepts/signals/traces/)

As mentioned in [motivation](#motivation), traces allows the developers to understand the full "path" a request takes in the application and navigate through a microservice architecture. Traces include [Spans](https://opentelemetry.io/docs/concepts/signals/traces/#spans) which are unit of works or operation in the ecosystem that include the following information:

- Name
- Parent span ID (empty for root spans)
- Start and End Timestamps
- [Span Context](https://opentelemetry.io/docs/concepts/signals/traces/#span-context)
- [Attributes](https://opentelemetry.io/docs/concepts/signals/traces/#attributes)
- [Span Events](https://opentelemetry.io/docs/concepts/signals/traces/#span-events)
- [Span Links](https://opentelemetry.io/docs/concepts/signals/traces/#span-links)
- [Span Status](https://opentelemetry.io/docs/concepts/signals/traces/#span-status)

The spans can be correlated with each other and assembled into a trace using [context propagation](https://opentelemetry.io/docs/concepts/signals/traces/#context-propagation).

### Example of a trace in a microservice architecture

#### Architecture

![architecture](./system-architecture-example.png)

#### Data visualization using Jaeger

![jaeger](./system-architecture-trace-timeline.png)

## OpenTelemetry Semantic Conventions

### Span name

[OpenTelemetry Trace Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/general/trace/) (at the time of this writing, it's on version 1.24.0) defines multi-purpose semantic conventions regarding tracing for different components and protocols (e.g.: Database, HTTP, Messaging, etc.)

For C# driver, the focus is the [semantic conventions for database client calls](https://opentelemetry.io/docs/specs/semconv/database/database-spans/) for the generic database attributes, and the [semantic conventions for Cassandra](https://opentelemetry.io/docs/specs/semconv/database/cassandra/) for the specific Cassandra attributes.

According to the specification, the span name "SHOULD be set to a low cardinality value representing the statement executed on the database. It MAY be a stored procedure name (without arguments), DB statement without variable arguments, operation name, etc.".\
The specification also (and only) specifies the span name for SQL databases:\
"Since SQL statements may have very high cardinality even without arguments, SQL spans SHOULD be named the following way, unless the statement is known to be of low cardinality:\
`<db.operation> <db.name>.<db.sql.table>`, provided that `db.operation` and `db.sql.table` are available. If `db.sql.table` is not available due to its semantics, the span SHOULD be named `<db.operation> <db.name>`. It is not recommended to attempt any client-side parsing of `db.statement` just to get these properties,
they should only be used if the library being instrumented already provides them. When it's otherwise impossible to get any meaningful span name, `db.name` or the tech-specific database name MAY be used."

To avoid parsing the statement, the **span name** in this implementation has the **keyspace name** as its value.

### Span attributes

This implementation includes, by default, the **required** attributes for Database, and Cassandra spans.\
`server.address` and `server.port`, despite only **recommended**, are included to give information regarding the client connection.\
`db.statement` is optional given that this attribute may contain sensitive information:

| Attribute  | Description  | Type | Required | Supported Values|
|---|---|---|---|---|
| span.kind | Describes the relationship between the Span, its parents, and its children in a Trace. | string | true | client |
| db.system | An identifier for the database management system (DBMS) product being used. | string | true | cassandra |
| db.name | The keyspace name in Cassandra. | string | true | *keyspace in use* |
| db.statement | The database statement being executed. | string | false | *database statement in use* |
| server.address | Name of the database host. | string | true | e.g.: example.com; 10.1.2.80; /tmp/my.sock |
| server.port | Server port number. Used in case the port being used is not the default | int | false | e.g.: 9445 |

## Usage

### Package installation

The OpenTelemetry implementation is included in the package `CassandraCSharpDriver.OpenTelemetry`.

### Cluster Builder

The extension method `AddOpenTelemetryInstrumentation()` is available in the cluster builder, so the traces can be exported on database operations.

```csharp
var cluster = Cluster.Builder()
    .AddContactPoint("127.0.0.1")
    .WithSessionName("session-name")
    .AddOpenTelemetryInstrumentation()
    .Build();
```

The extension method also includes the option to enable the database statement that is disabled by default:

```csharp
var cluster = Cluster.Builder()
    .AddContactPoint("127.0.0.1")
    .WithSessionName("session-name")
    .AddOpenTelemetryInstrumentation(options => options.SetDbStatement = true)
    .Build();
```

### Capture Cassandra activity

When setting up the tracer provider, it is necessary to include the Cassandra source for the activity to be captured. The activity source name is available in the property `CassandraInstrumentation.ActivitySourceName` that is included in the `CassandraCSharpDriver.OpenTelemetry` package.

Example:

```csharp
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
 .AddSource(CassandraInstrumentation.ActivitySourceName)
 .AddConsoleExporter()
 .Build();
```


Explain the proposal as if it was already included in the language and you were teaching it to another Rust programmer. That generally means:

- Introducing new named concepts.
- Explaining the feature largely in terms of examples.
- Explaining how Rust programmers should *think* about the feature, and how it should impact the way they use Rust. It should explain the impact as concretely as possible.
- If applicable, provide sample error messages, deprecation warnings, or migration guidance.
- If applicable, describe the differences between teaching this to existing Rust programmers and new Rust programmers.
- Discuss how this impacts the ability to read, understand, and maintain Rust code. Code is read and modified far more often than written; will the proposed feature make code easier to maintain?

For implementation-oriented RFCs (e.g. for compiler internals), this section should focus on how compiler contributors should think about the change, and give examples of its concrete impact. For policy RFCs, this section should provide an example-driven introduction to the policy, and explain its impact in concrete terms.

# Reference-level explanation
[reference-level-explanation]: #reference-level-explanation

Referenciar que este projeto usa .NET Standard 2.0

This is the technical portion of the RFC. Explain the design in sufficient detail that:

- Its interaction with other features is clear.
- It is reasonably clear how the feature would be implemented.
- Corner cases are dissected by example.

The section should return to the examples given in the previous section, and explain more fully how the detailed proposal makes those examples work.

# Drawbacks
[drawbacks]: #drawbacks

Why should we *not* do this?

# Rationale and alternatives
[rationale-and-alternatives]: #rationale-and-alternatives


Falar que o racional poderia ser
1) Incluir telemetria por padrão como acontece no ElasticSearch, ao invés de ser um package à parte
2) Inclur traces no projeto de contrib que só tem métricas

- Why is this design the best in the space of possible designs?
- What other designs have been considered and what is the rationale for not choosing them?
- What is the impact of not doing this?
- If this is a language proposal, could this be done in a library or macro instead? Does the proposed change make Rust code easier or harder to read, understand, and maintain?

# Prior art
[prior-art]: #prior-art

Incluir outros projetos de databases de OpenTelemetry .NET aqui e outros projetos de Cassandra noutras linguagens.

Discuss prior art, both the good and the bad, in relation to this proposal.
A few examples of what this can include are:

- For language, library, cargo, tools, and compiler proposals: Does this feature exist in other programming languages and what experience have their community had?
- For community proposals: Is this done by some other community and what were their experiences with it?
- For other teams: What lessons can we learn from what other communities have done here?
- Papers: Are there any published papers or great posts that discuss this? If you have some relevant papers to refer to, this can serve as a more detailed theoretical background.

This section is intended to encourage you as an author to think about the lessons from other languages, provide readers of your RFC with a fuller picture.
If there is no prior art, that is fine - your ideas are interesting to us whether they are brand new or if it is an adaptation from other languages.

Note that while precedent set by other languages is some motivation, it does not on its own motivate an RFC.
Please also take into consideration that rust sometimes intentionally diverges from common language features.

# Unresolved questions
[unresolved-questions]: #unresolved-questions

- TBD

# Future possibilities
[future-possibilities]: #future-possibilities

## Traces

As referred in [semantic conventions section](#semantic-conventions), there are recommended attributes that are not included in this proposal that may be useful for the users of Cassandra telemetry and can be something to look at in the future iterations of this feature:

- [Cassandra Call-level attributes](https://opentelemetry.io/docs/specs/semconv/database/cassandra/#call-level-attributes)
- [Database Call-level attributes](https://opentelemetry.io/docs/specs/semconv/database/database-spans/#call-level-attributes)
- [Database Connection-level attributes](https://opentelemetry.io/docs/specs/semconv/database/database-spans/#connection-level-attributes)

## Metrics

As the industry is moving to adopt OpenTelemetry, the export of metrics using this standard may be something useful for the users of Cassandra C# Driver. Although the [semantic conventions for database metrics](https://opentelemetry.io/docs/specs/semconv/database/database-metrics/) are still in experimental status, there are already some community-lead efforts [available in the opentelemetry-dotnet-contrib repositories](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Cassandra).
