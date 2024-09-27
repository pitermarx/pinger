module Routes
open Oxpecker
open Oxpecker.ViewEngine
open System
open Microsoft.AspNetCore.Http

let chart (config: Models.Config) =
    html() {
        head() {
            title() { "Latency Chart for " + config.Site }
            script(src="https://cdn.plot.ly/plotly-latest.min.js") 
        }
        body() {
            label(){
                "Show Last "
                input(id="from", type'="number", value="120")
                    .attr("onchange", "redraw(+event.target.value)")
                " Minutes"
            }
            div(id="chart") 
            script(src="getData.js")
        }
    }

let getData (date: float) (ctx: HttpContext) =
    let from = DateTime.UnixEpoch.AddMilliseconds(date).ToLocalTime()
    let data = ctx.GetService<LatencyData.LatencyData>()
    ctx.WriteJson(data.getData from)