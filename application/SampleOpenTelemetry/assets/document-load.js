import { diag, DiagConsoleLogger, DiagLogLevel, context, propagation, ROOT_CONTEXT, trace } from "@opentelemetry/api";
import { SemanticResourceAttributes } from "@opentelemetry/semantic-conventions";
import { Resource } from "@opentelemetry/resources";
import { B3Propagator } from '@opentelemetry/propagator-b3';
import { CompositePropagator, W3CTraceContextPropagator, W3CBaggagePropagator } from '@opentelemetry/core';
import { WebTracerProvider } from "@opentelemetry/sdk-trace-web";
import { ConsoleSpanExporter, SimpleSpanProcessor } from "@opentelemetry/sdk-trace-base";
import { OTLPTraceExporter } from "@opentelemetry/exporter-trace-otlp-http";
import { DocumentLoadInstrumentation } from "@opentelemetry/instrumentation-document-load";
import { XMLHttpRequestInstrumentation } from "@opentelemetry/instrumentation-xml-http-request";
import { UserInteractionInstrumentation } from "@opentelemetry/instrumentation-user-interaction";
import { ZoneContextManager } from "@opentelemetry/context-zone";
import { registerInstrumentations } from "@opentelemetry/instrumentation";

diag.setLogger(
    new DiagConsoleLogger(),
    DiagLogLevel.VERBOSE
);

const provider = new WebTracerProvider({
    resource: new Resource({
        [SemanticResourceAttributes.SERVICE_NAME]: "SampleOpenTelemetry"
    })
});

const config = {
    url: "http://localhost:4318/v1/traces",
    headers: {},
    concurrencyLimit: 10
};

provider.addSpanProcessor(new SimpleSpanProcessor(new ConsoleSpanExporter()));
provider.addSpanProcessor(new SimpleSpanProcessor(new OTLPTraceExporter(config)));

provider.register({
    contextManager: new ZoneContextManager(),
    propagator: new CompositePropagator({
        propagators: [
            new W3CTraceContextPropagator(),
            new W3CBaggagePropagator()
        ],
    }),
});

registerInstrumentations({
    instrumentations: [
        //new DocumentLoadInstrumentation(),
        //new UserInteractionInstrumentation(),
        new XMLHttpRequestInstrumentation({
            propagateTraceHeaderCorsUrls: /https:\/\/localhost:7259.+/,
            applyCustomAttributesOnSpan: (span) => {
                span.setAttribute('product.id', 12345);
            }
        })
    ]
});

const tracer = trace.getTracer('SampleOpenTelemetry');

window.onload = () => {
    const btn = document.getElementById('ajax-call-button');

    btn.addEventListener('click', () => {

        const metaElement = Array.from(document.getElementsByTagName('meta')).find(e => e.getAttribute('name') === 'traceparent');
        const traceparent = (metaElement && metaElement.content) || '';

        //propagation.setBaggage(context.active(), propagation.createBaggage({ "product.id": 12345 }));

        const span = tracer.startSpan("Calling compute for FE...", {}, propagation.extract(ROOT_CONTEXT, { traceparent }));

        span.setAttribute("product.id", 12345);

        context.with(trace.setSpan(context.active(), span), () => {
            var xhttp = new XMLHttpRequest();
            xhttp.open('GET', 'https://localhost:7259/compute-for-frontend', false);
            xhttp.send();
        });

        span.end();
    });
}