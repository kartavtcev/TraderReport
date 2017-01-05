namespace TraderReport.Log

module Log =

    open System
    open log4net

    let private _log = LogManager.GetLogger("TraderReport")
    let error(exn: Exception) = _log.Error(exn.Message, exn)


