<template>
    <HqLayout>
        <div class="row">
            <div class="col-md-6 col-xs-12">
                <div class="panel"
                    :class="panelStatus">
                    <div class="panel-heading">
                        <h3 v-if="report == null">
                            Waiting for health check
                        </h3>
                        <h3 v-else>
                            Health status: {{report.status}}
                        </h3>
                    </div>
                    <div class="panel-body">
                        <ul class="list-group">
                            <a class="list-group-item"
                                v-for="entry in entries"
                                :key="entry.name"
                                :href="entry.item.data.url"
                                :class="itemStatus(entry.item)">
                                <h4>{{entry.name}}</h4>
                                <p>{{entry.item.description}}</p>
                                <div class="well"
                                    v-if="entry.item.exception">{{ entry.item.exception.Message}}</div>
                            </a>
                        </ul>
                    </div>
                </div>
            </div>
            <div class="col-md-6 col-xs-12">
                <div class="panel panel-default">
                    <div class="panel-heading">
                        <h3>Server status</h3>
                    </div>
                    <div class="panel-body">
                        <ul class="list-group">
                            <li v-if="metrics.length == 0"
                                class="list-group-item">Loading...</li>
                            <li class="list-group-item"
                                v-for="metric in metrics"
                                :key="metric.name">
                                <b>{{metric.name}}: </b>{{metric.value}}
                            </li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>
    </HqLayout>
</template>

<script>

export default {
    data() {
        return {
            report: null,
            metrics: [],
            errors: [],
        }
    },
    mounted() {
        this.getHealth()
    },

    computed: {
        panelStatus() {
            if(this.report == null) return [ ]
            return [ 'panel-' + this.statusToBs(this.report.status)]
        },
        entries() {
            if(this.report == null) return []

            return Object.keys(this.report.entries).map(name => {
                const item = this.report.entries[name]
                return { name, item }
            })
        },
    },

    methods: {
        getHealth() {
            const self = this
            this.$hq.ControlPanel.getHealthResult().then(response => {
                self.report = response.data
                setTimeout(this.getHealth, 1000)
            })

            this.$hq.ControlPanel.getMetricsState().then(response => {
                self.metrics = response.data
            })
        },

        statusToBs(status) {
            switch(status) {
                case 'Healthy': return 'success'
                case 'Degraded': return 'warning'
                case 'Unhealthy': return 'danger'
            }
        },
        itemStatus(entry) {
            return ['list-group-item-' + this.statusToBs(entry.status)]
        },
    },
}
</script>

<style></style>
