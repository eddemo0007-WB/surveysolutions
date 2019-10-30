import Vue from "vue"

function forEachIfNeeded(data, each) {
    if (Array.isArray(data)) {
        data.forEach(section => {
            each(section)
        })
    } else {
        each(data)
    }
}

export function batchedAction(callback, fetchAction = "fetch", limit = null) {
    let queue = []

    return (ctx, data) => {
        data = data || null
        
        if (fetchAction != null) {
            forEachIfNeeded(data, id => ctx.dispatch(fetchAction, { id }))
        }

        const isQueueWereEmpty = queue.length === 0

        forEachIfNeeded(data, item => queue.push(item))

        if (isQueueWereEmpty) {
            Vue.nextTick(() => {
                const ids = queue
                queue = []
                return callback(ctx, ids)
            })
        }

        if (limit && queue.length > limit) {
            const ids = queue
            queue = []
            return callback(ctx, ids)
        }
    }
}
