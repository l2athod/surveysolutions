function modal(action) {
    require.ensure(["bootbox", "bootstrap-sass/assets/javascripts/bootstrap/modal"], r => {
        const jQuery = require("jquery")
        const $ = (window as any).$ = (window as any).jQuery = jQuery
        require("bootstrap-sass/assets/javascripts/bootstrap/modal")
        action(require("bootbox"))
    }, "libs")
}

export default {
    confirm(message, callback) {
        modal(box => box.confirm(message, callback))
    },
    alert(options) {
        modal(box => box.alert(options))
    }
}
