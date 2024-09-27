module Routes
open Oxpecker
open Oxpecker.ViewEngine

let private chart =
    html() {
        head() {
            title() { "Latency Chart" }
            script(src="https://cdn.plot.ly/plotly-latest.min.js")
        }
        body() {
            label(){
                "Show Last "
                input(id="from", type'="number", value="120")
                    .attr("onchange", "redraw(+event.target.value)")
                "Minutes"
            }
            div(id="chart")
            script(src="getData.js")
        }
    }

let private getData (date: float) =
    let from = System.DateTime.UnixEpoch.AddMilliseconds(date).ToLocalTime()
    json <| LatencyData.getData from

let endpoints = [
    GET [
        route "/" <| htmlView chart
        routef "/data/{%f}" <| getData
    ]
]