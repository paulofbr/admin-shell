import { beforeEach, describe, expect, it } from 'vitest'
import { defineComponent, h } from 'vue'
import {
  listPluginComponents,
  registerPluginComponent,
  resolvePluginComponent,
} from '@/utils/pluginComponentRegistry'

const FirstComponent = defineComponent({
  name: 'FirstPluginComponent',
  render: () => h('div', 'first'),
})

const SecondComponent = defineComponent({
  name: 'SecondPluginComponent',
  render: () => h('div', 'second'),
})

describe('pluginComponentRegistry', () => {
  beforeEach(() => {
    registerPluginComponent('FirstPluginComponent', FirstComponent)
    registerPluginComponent('SecondPluginComponent', SecondComponent)
  })

  it('resolves registered plugin components by name', () => {
    expect(resolvePluginComponent('FirstPluginComponent')).toBe(FirstComponent)
    expect(resolvePluginComponent('SecondPluginComponent')).toBe(SecondComponent)
  })

  it('returns undefined for unregistered components', () => {
    expect(resolvePluginComponent('MissingPluginComponent')).toBeUndefined()
  })

  it('lists registered component names', () => {
    expect(listPluginComponents()).toEqual([
      'FirstPluginComponent',
      'SecondPluginComponent',
    ])
  })

  it('overwrites existing component registrations', () => {
    const ReplacementComponent = defineComponent({
      name: 'ReplacementPluginComponent',
      render: () => h('div', 'replacement'),
    })

    registerPluginComponent('FirstPluginComponent', ReplacementComponent)

    expect(resolvePluginComponent('FirstPluginComponent')).toBe(ReplacementComponent)
  })
})
