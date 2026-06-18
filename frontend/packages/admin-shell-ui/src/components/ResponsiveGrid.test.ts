import { describe, expect, it } from 'vitest'
import { defineComponent, markRaw, nextTick } from 'vue'
import { mount } from '@vue/test-utils'
import ResponsiveGrid from '@admin-shell/ui/ResponsiveGrid.vue'

const columns = [
  { id: 'name', label: 'Name', prop: 'name' },
]

const EditorStub = markRaw(defineComponent({
  name: 'EditorStub',
  emits: ['saved'],
  template: '<button type="button" @click="$emit(\'saved\', { id: 1, name: \'Saved user\' })">Save</button>',
}))

describe('ResponsiveGrid editor integration', () => {
  it('emits editor-saved with the original saved value when saveToProperty is not set', async () => {
    const wrapper = mount(ResponsiveGrid, {
      props: {
        data: [{ id: 1, name: 'Paulo' }],
        columns,
        editMode: 'popup',
        editorComponent: EditorStub,
      },
      global: {
        stubs: {
          ElDialog: { template: '<div><slot /></div>' },
          ElTable: true,
          ElTableColumn: true,
          ElPagination: true,
          ElSkeleton: true,
          ElButton: true,
          ElInput: true,
          ElSelect: true,
          ElOption: true,
          ElFormItem: true,
          ElDatePicker: true,
          ElSwitch: true,
        },
        directives: {
          loading: () => ({}),
        },
      },
    })

    const grid = wrapper.vm as unknown as { openEditor: (row: unknown) => Promise<void> }
    await grid.openEditor({ id: 1, name: 'Paulo' })
    await nextTick()

    await wrapper.findComponent(EditorStub).find('button').trigger('click')

    expect(wrapper.emitted('editor-saved')).toHaveLength(1)
    expect(wrapper.emitted('editor-saved')?.[0]?.[0]).toEqual({ id: 1, name: 'Saved user' })
  })

  it('adds grid data to the configured saveToProperty when saving from the editor', async () => {
    const gridData = [{ id: 1, name: 'Paulo' }]

    const wrapper = mount(ResponsiveGrid, {
      props: {
        data: gridData,
        columns,
        editMode: 'popup',
        editorComponent: EditorStub,
        saveToProperty: 'gridRows',
      },
      global: {
        stubs: {
          ElDialog: { template: '<div><slot /></div>' },
          ElTable: true,
          ElTableColumn: true,
          ElPagination: true,
          ElSkeleton: true,
          ElButton: true,
          ElInput: true,
          ElSelect: true,
          ElOption: true,
          ElFormItem: true,
          ElDatePicker: true,
          ElSwitch: true,
        },
        directives: {
          loading: () => ({}),
        },
      },
    })

    const grid = wrapper.vm as unknown as { openEditor: (row: unknown) => Promise<void> }
    await grid.openEditor(null)
    await nextTick()

    await wrapper.findComponent(EditorStub).find('button').trigger('click')

    expect(wrapper.emitted('editor-saved')).toHaveLength(1)
    expect(wrapper.emitted('editor-saved')?.[0]?.[0]).toEqual({
      id: 1,
      name: 'Saved user',
      gridRows: gridData,
    })
  })
})
