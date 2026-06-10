type EventHandler<T = unknown> = (data: T) => void

class EventBus {
  private handlers: Map<string, Set<EventHandler>> = new Map()

  publish<T>(event: string, data: T): void {
    // Notify exact match subscribers
    const exactHandlers = this.handlers.get(event)
    if (exactHandlers) {
      exactHandlers.forEach((handler) => {
        try {
          handler(data)
        } catch (error) {
          console.error(`EventBus error in handler for "${event}":`, error)
        }
      })
    }

    // Notify wildcard subscribers (e.g., 'user:*' matches 'user:login')
    const colonIndex = event.indexOf(':')
    if (colonIndex !== -1) {
      const namespace = event.substring(0, colonIndex)
      const wildcardEvent = `${namespace}:*`
      const wildcardHandlers = this.handlers.get(wildcardEvent)
      if (wildcardHandlers) {
        wildcardHandlers.forEach((handler) => {
          try {
            handler(data)
          } catch (error) {
            console.error(
              `EventBus error in wildcard handler for "${wildcardEvent}":`,
              error,
            )
          }
        })
      }
    }
  }

  subscribe<T>(event: string, handler: EventHandler<T>): () => void {
    if (!this.handlers.has(event)) {
      this.handlers.set(event, new Set())
    }
    this.handlers.get(event)!.add(handler as EventHandler)

    // Return unsubscribe function
    return () => {
      const handlers = this.handlers.get(event)
      if (handlers) {
        handlers.delete(handler as EventHandler)
        if (handlers.size === 0) {
          this.handlers.delete(event)
        }
      }
    }
  }

  clear(): void {
    this.handlers.clear()
  }
}

export const eventBus = new EventBus()
export default eventBus