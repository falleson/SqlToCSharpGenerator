using System;

/// <summary>
/// Order实体
/// </summary>
public class Order
{
    /// <summary>
    ///订单ID，唯一编号，自增标志列
    /// </summary>
    public int OrderID { get; set; }

    /// <summary>
    ///配送信息ID，外键参照SHIPMENT
    /// </summary>
    public int ShipmentID { get; set; }

    /// <summary>
    ///用户编号，外键参照Client_Basic
    /// </summary>
    public int ClientSN { get; set; }

    /// <summary>
    ///订单积分
    /// </summary>
    public int TotalPoints { get; set; }

    /// <summary>
    ///商品ID，外键参照Product
    /// </summary>
    public int ProductID { get; set; }

    /// <summary>
    ///订单生成时的商品名称
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    ///商品序列号
    /// </summary>
    public string ProductSN { get; set; }

    /// <summary>
    ///商品数量
    /// </summary>
    public int Quatity { get; set; }

    /// <summary>
    ///0-不需要（虚拟物品，如抽奖）,1-需要（实物）
    /// </summary>
    public bool IsShipment { get; set; }

    /// <summary>
    ///订单生成日期，初始值为系统当前日期
    /// </summary>
    public DateTime OrderDate { get; set; }

    /// <summary>
    ///0：订单未确定（初始值）1：订单已确定，未付款2：已发货，等待确认收货3：用户确认收货，交易关闭
    /// </summary>
    public int OrderStatus { get; set; }

    /// <summary>
    ///没有字段说明信息
    /// </summary>
    public int CampaignSN { get; set; }

    /// <summary>
    ///没有字段说明信息
    /// </summary>
    public string Remark { get; set; }

    /// <summary>
    ///没有字段说明信息
    /// </summary>
    public int? OrderSource { get; set; }


}