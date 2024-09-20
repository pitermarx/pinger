open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection

open Falco
open Falco.Markup
open Falco.Routing
open Falco.HostBuilder

let chart =
    Elem.html [] [
        Elem.head [] [
            Elem.title [] [ Text.raw "Latency Chart" ]
            Elem.script [ Attr.src "https://cdn.plot.ly/plotly-latest.min.js" ] []
        ]
        Elem.body [] [
            Elem.div [ Attr.id "chart" ] []
            Elem.script [] [
                Text.raw """
                fetch('/data')
                    .then(response => response.json())
                    .then(data => {
                        const x = data.map(d => d.Time);
                        const build = (y, name) => ({ x, y, type: 'scatter', mode: 'lines+markers', name })

                        const traces = [
                            build(data.map(d => d.Latency), 'Latency'),
                            build(data.map(d => d.Max), 'Max'),
                            build(data.map(d => d.Min), 'Min'),
                            build(data.map(d => d.Avg), 'Avg')
                        ];

                        const layout = {
                            title: 'Latency Over Time',
                            xaxis: { title: 'Time' },
                            yaxis: { title: 'Latency (ms)' }
                        };

                        Plotly.newPlot('chart', traces, layout);
                    });
                """
            ]
        ]
    ]

let handler : HttpHandler =
    Response.ofHtml chart

let getData: HttpHandler = fun ctx ->
    Response.ofJson (LatencyData.getData()) ctx

webHost [||] {
    add_service _.AddSingleton<IHostedService, Pinger.PingService>()

    endpoints [
        get "/" handler
        get "/data" getData
    ]
}
