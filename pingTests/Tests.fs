open Faqt
open Xunit

[<Fact>]
let ``LatencyData should be empty`` () =
    LatencyData.clear()
    LatencyData.getData().Should().BeEmpty()

[<Fact>]
let ``LatencyData should not be empty`` () =
    LatencyData.clear()
    LatencyData.update (Some 1)
    LatencyData.getData().Should().NotBeEmpty().And.HaveLength(1)

[<Fact>]
let ``LatencyData should be empty after clear`` () =
    LatencyData.clear()
    LatencyData.update (Some 1)
    LatencyData.clear()
    LatencyData.getData().Should().BeEmpty()

[<Fact>]
let ``LatencyData should be correctly calculated`` () =
    LatencyData.clear()
    for i in 1..10 do
        LatencyData.update (Some i)
    LatencyData.getData().Should().HaveLength(10)