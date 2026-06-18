import { describe, expect, it, vi } from 'vitest'
import { render, fireEvent, screen } from '@testing-library/vue'
import ElementPlus from 'element-plus'
import EntityEditor from '@admin-shell/ui/EntityEditor.vue'
import type { ExtensionField } from '@admin-shell/ui/types'

describe('EntityEditor', () => {
  it('renders title, subtitle and default actions', async () => {
    const onSave = vi.fn()
    const onCancel = vi.fn()

    render(EntityEditor, {
      global: { plugins: [ElementPlus] },
      props: {
        title: 'Edit User',
        subtitle: 'Update user details',
        onSave,
        onCancel,
      },
      slots: {
        default: '<p>User content</p>',
      },
    })

    expect(screen.getByRole('heading', { name: 'Edit User' })).toBeTruthy()
    expect(screen.getByText('Update user details')).toBeTruthy()
    expect(screen.getByText('User content')).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Save' })).toBeTruthy()

    await fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(onSave).toHaveBeenCalledTimes(1)
    await fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))
    expect(onCancel).toHaveBeenCalledTimes(1)
  })

  it('uses custom labels and hides the save button when requested', () => {
    render(EntityEditor, {
      global: { plugins: [ElementPlus] },
      props: {
        title: 'Permissions',
        cancelLabel: 'Close',
        saveLabel: 'Save Permissions',
        showSave: false,
      },
      slots: {
        default: '<p>Permissions content</p>',
      },
    })

    expect(screen.getByRole('button', { name: 'Close' })).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Save Permissions' })).not.toBeTruthy()
  })

  it('renders toolbar and footer slots', () => {
    render(EntityEditor, {
      global: { plugins: [ElementPlus] },
      props: {
        title: 'Order',
      },
      slots: {
        default: '<p>Order content</p>',
        'toolbar-actions': '<button type="button">Toolbar action</button>',
        actions: '<button type="button">Custom action</button>',
      },
    })

    expect(screen.getByRole('button', { name: 'Toolbar action' })).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Custom action' })).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Cancel' })).not.toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Save' })).not.toBeTruthy()
  })

  it('renders extension fields from formModel', () => {
    const formModel = {
      extensionFields: [
        {
          name: 'department',
          label: 'Department',
          type: 'string',
          required: true,
          value: 'Engineering',
        },
        {
          name: 'location',
          label: 'Location',
          type: 'string',
          required: false,
          value: 'Lisbon',
          slot: 'details',
        },
      ] satisfies ExtensionField[],
    }

    render(EntityEditor, {
      global: { plugins: [ElementPlus] },
      props: {
        title: 'User',
        formModel,
      },
    })

    expect(screen.getByPlaceholderText('Department')).toBeTruthy()
    expect(screen.getByPlaceholderText('Location')).toBeTruthy()
  })
})
