import { useState, type FormEvent, type ReactNode } from 'react'
import { useForm, useWatch, type FieldPath } from 'react-hook-form'
import { Link } from 'react-router'
import { SCHEME_STATUSES } from '../../api/schemeQuery'
import { ApiError } from '../../api/client'
import ErrorBanner from '../../components/ErrorBanner'
import LoadingState from '../../components/LoadingState'
import { formatMoney, formatNumber, statusLabels } from '../../lib/format'
import type { CreateSchemeRequest } from '../../types/scheme'
import {
  isFormField,
  previewLineValue,
  previewScheme,
  TENURES,
  toRequest,
  type SchemeFormValues,
} from './schemeFormValues'
import {
  validateAverageValue,
  validateHomes,
  validateName,
  validateSiteAddress,
  validateTotalHomes,
} from './validation'

const NO_VALUE = '—'

interface SchemeFormProps {
  defaultValues: SchemeFormValues
  submitLabel: string
  backTo: string
  onSubmit: (scheme: CreateSchemeRequest) => Promise<void>
  notice?: ReactNode
}

interface FieldErrorProps {
  id: string
  message?: string
}

function FieldError({ id, message }: FieldErrorProps) {
  return message ? (
    <p id={id} className="field-error">
      {message}
    </p>
  ) : null
}

const describedBy = (id: string, message?: string) =>
  message ? { 'aria-invalid': true, 'aria-describedby': id } : {}

export default function SchemeForm({
  defaultValues,
  submitLabel,
  backTo,
  onSubmit,
  notice,
}: SchemeFormProps) {
  const {
    register,
    handleSubmit,
    setError,
    setFocus,
    trigger,
    getFieldState,
    control,
    formState: { errors, isSubmitting },
  } = useForm<SchemeFormValues>({ defaultValues, mode: 'onTouched' })

  // Kept apart from the form's own errors, because its checks would erase these when a
  // field loses focus. The server's verdict stands until the value it judged changes.
  const [serverErrors, setServerErrors] = useState<Record<string, string>>({})

  const clearServerErrors = (...paths: string[]) =>
    setServerErrors((current) =>
      paths.some((path) => path in current)
        ? Object.fromEntries(
            Object.entries(current).filter(([path]) => !paths.includes(path)),
          )
        : current,
    )

  const status = useWatch({ control, name: 'status' })
  const tenureMix = useWatch({ control, name: 'tenureMix' })
  const preview = previewScheme(tenureMix)
  const totalHomesError = validateTotalHomes(
    status,
    TENURES.map(({ key }) => tenureMix[key].homes),
  )

  const showServerErrors = (error: unknown) => {
    const fieldErrors = error instanceof ApiError ? error.problem?.errors : null

    if (!fieldErrors || Object.keys(fieldErrors).length === 0) {
      setError('root.server', {
        message:
          error instanceof ApiError
            ? error.message
            : 'Something went wrong. Please try again.',
      })
      return
    }

    const onFields: Record<string, string> = {}
    const otherMessages: string[] = []

    Object.entries(fieldErrors).forEach(([key, messages]) => {
      const message = messages.join(' ')

      if (key === 'tenureMix') {
        setError('root.tenureMix', { message })
      } else if (isFormField(key)) {
        onFields[key] = message
      } else {
        otherMessages.push(message)
      }
    })

    if (otherMessages.length > 0) {
      setError('root.server', { message: otherMessages.join(' ') })
    }

    setServerErrors(onFields)

    const [firstField] = Object.keys(onFields)

    if (firstField) {
      setFocus(firstField as FieldPath<SchemeFormValues>)
    }
  }

  const submit = handleSubmit(async (values) => {
    setServerErrors({})

    const mixError = validateTotalHomes(
      values.status,
      TENURES.map(({ key }) => values.tenureMix[key].homes),
    )

    if (mixError) {
      setFocus(`tenureMix.${TENURES[0].key}.homes`)
      return
    }

    try {
      await onSubmit(toRequest(values))
    } catch (error) {
      showServerErrors(error)
    }
  })

  // A disabled button doesn't stop every submit, and a second save would be a duplicate.
  const submitOnce = (event: FormEvent<HTMLFormElement>) => {
    if (isSubmitting) {
      event.preventDefault()
      return
    }

    void submit(event)
  }

  const nameError = errors.name?.message ?? serverErrors.name
  const addressError = errors.siteAddress?.message ?? serverErrors.siteAddress
  const statusError = errors.status?.message ?? serverErrors.status
  const mixMessage = totalHomesError ?? errors.root?.tenureMix?.message

  return (
    <form className="scheme-form" onSubmit={submitOnce} noValidate>
      <div className="form-field">
        <label htmlFor="scheme-name">Name</label>
        <input
          id="scheme-name"
          type="text"
          autoComplete="off"
          {...describedBy('scheme-name-error', nameError)}
          {...register('name', {
            validate: validateName,
            // A duplicate is reported on both fields, and changing either can fix it.
            onChange: () => clearServerErrors('name', 'siteAddress'),
          })}
        />
        <FieldError id="scheme-name-error" message={nameError} />
      </div>

      <div className="form-field">
        <label htmlFor="scheme-address">Site address</label>
        <input
          id="scheme-address"
          type="text"
          autoComplete="off"
          {...describedBy('scheme-address-error', addressError)}
          {...register('siteAddress', {
            validate: validateSiteAddress,
            onChange: () => clearServerErrors('name', 'siteAddress'),
          })}
        />
        <FieldError id="scheme-address-error" message={addressError} />
      </div>

      <div className="form-field">
        <label htmlFor="scheme-status">Status</label>
        <select
          id="scheme-status"
          {...describedBy('scheme-status-error', statusError)}
          {...register('status', {
            onChange: () => clearServerErrors('status'),
          })}
        >
          {SCHEME_STATUSES.map((option) => (
            <option key={option} value={option}>
              {statusLabels[option]}
            </option>
          ))}
        </select>
        <FieldError id="scheme-status-error" message={statusError} />
      </div>

      <div className="table-container">
        <table className="tenure-table">
          <caption>Tenure mix</caption>
          <thead>
            <tr>
              <th scope="col">Tenure</th>
              <th scope="col">Homes</th>
              <th scope="col">Average value (£)</th>
              <th scope="col">Line value (preview)</th>
            </tr>
          </thead>
          <tbody>
            {TENURES.map(({ key, label }) => {
              const homesPath = `tenureMix.${key}.homes` as const
              const valuePath = `tenureMix.${key}.averageValue` as const
              const homesId = `${key}-homes`
              const valueId = `${key}-value`
              const homesError =
                errors.tenureMix?.[key]?.homes?.message ?? serverErrors[homesPath]
              const valueError =
                errors.tenureMix?.[key]?.averageValue?.message ??
                serverErrors[valuePath]
              const lineValue = previewLineValue(tenureMix[key])

              return (
                <tr key={key}>
                  <th scope="row">{label}</th>
                  <td>
                    <label htmlFor={homesId} className="visually-hidden">
                      {label} homes
                    </label>
                    <input
                      id={homesId}
                      type="text"
                      inputMode="numeric"
                      autoComplete="off"
                      {...describedBy(`${homesId}-error`, homesError)}
                      {...register(homesPath, {
                        validate: validateHomes,
                        onChange: () => {
                          clearServerErrors(homesPath, valuePath)

                          // Only once visited, so a field isn't flagged before it's reached.
                          if (getFieldState(valuePath).isTouched) {
                            void trigger(valuePath)
                          }
                        },
                      })}
                    />
                    <FieldError id={`${homesId}-error`} message={homesError} />
                  </td>
                  <td>
                    <label htmlFor={valueId} className="visually-hidden">
                      {label} average value
                    </label>
                    <input
                      id={valueId}
                      type="text"
                      inputMode="decimal"
                      autoComplete="off"
                      {...describedBy(`${valueId}-error`, valueError)}
                      {...register(valuePath, {
                        validate: (value, form) =>
                          validateAverageValue(value, form.tenureMix[key].homes),
                        onChange: () => clearServerErrors(valuePath),
                      })}
                    />
                    <FieldError id={`${valueId}-error`} message={valueError} />
                  </td>
                  <td>{lineValue === null ? NO_VALUE : formatMoney(lineValue)}</td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      <p className="form-preview">
        <strong>Preview:</strong>{' '}
        {preview.totalHomes === null
          ? NO_VALUE
          : formatNumber(preview.totalHomes)}{' '}
        homes,{' '}
        {preview.affordableHomes === null
          ? NO_VALUE
          : formatNumber(preview.affordableHomes)}{' '}
        affordable, total value{' '}
        {preview.totalValue === null ? NO_VALUE : formatMoney(preview.totalValue)}.
        The saved figures come from the server.
      </p>

      {mixMessage && (
        <div role="alert" className="error-banner">
          <p>{mixMessage}</p>
        </div>
      )}

      {errors.root?.server?.message && (
        <ErrorBanner message={errors.root.server.message} onRetry={submit} />
      )}

      {notice}

      {isSubmitting && <LoadingState label="Saving…" />}

      <div className="form-actions">
        {/* Keeps focus in the field on mouse down. Otherwise an error appearing as the
            field loses focus moves this button, and the click misses it. */}
        <button
          type="submit"
          disabled={isSubmitting}
          onMouseDown={(event) => event.preventDefault()}
        >
          {submitLabel}
        </button>
        <Link to={backTo}>Back to schemes</Link>
      </div>
    </form>
  )
}
