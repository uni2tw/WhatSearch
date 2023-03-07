using System;

namespace WhatSearch.DataModels
{
    public class UserEntity
    {
        public string Name { get; set; }
        public string Display { get; set; }
        public string Pic { get; set; }
        public string Token { get; set; }
        public int Status { get; set; }
        public int Admin { get; set; }
        public DateTime Create { get; set; }
        public DateTime Access { get; set; }
    }
}
