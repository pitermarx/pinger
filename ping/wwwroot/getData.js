function boot(from = 120) {
    const MS_PER_MINUTE = 60000;
        
    const build = (name, type='scatter', mode='lines+markers') => ({
        x: [],
        y: [],
        name,
        type,
        mode,
        update(data) {
            this.x = [...data.map(d => d.time), ...this.x]
            this.y = [...data.map(d => d[this.name.toLowerCase()]), ...this.y]
        }
    })

    const traces = [
        build('Latency'),
        build('Max'),
        build('Min'),
        build('Avg')
    ]

    const layout = {
        title: 'Latency Over Time',
        xaxis: { title: 'Time' },
        yaxis: { title: 'Latency (ms)' }
    }

    Plotly.newPlot('chart', traces, layout)
    t = updateData(120)

    return function startOver(newFrom) {
        from = newFrom
        traces.forEach(t => t.x = t.y = [])
        // clearTimeout(t)
        t = updateData()
    }

    async function updateData() {
        const x = traces[0].x
        const lastDate = !x.length ? +new Date() - (from * MS_PER_MINUTE) : +new Date(x[0]);
        const data = await fetch('/data/' + lastDate).then(d => d.json())
        traces.forEach(t => t.update(data))
        Plotly.redraw('chart')
        // return setTimeout(updateData, 5000)
    }
}

const redraw = boot()